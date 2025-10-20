package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io/ioutil"
	"log"
	"net/http"
	"os"
	"strings"
)

func main() {
	const configPath = "config.json"

	log.Println(">> Starting baseline permissions setup...")

	config, err := loadConfig(configPath)
	if err != nil {
		panic(err)
	}

	if config.Debug {
		log.Println("Configuration loaded:")
		log.Printf(" - API URL: %s", config.ApiUrl)
	}

	if config.Debug {
		log.Println("...")
	}

	/*
	  Fetch available roles
	  Check that required roles exist
	  if not, create them
	*/
	if config.Debug {
		log.Println(">> Consolidating roles list...")
	}

	availableRoles, err := fetchRoles(config)
	if err != nil {
		log.Fatalf("failed to fetch roles: %v", err)
	}

	for _, role := range config.Roles {
		if _, exists := availableRoles[strings.ToLower(role.Name)]; !exists {
			log.Printf("required role '%s' does not exist in the system; creating it...", role)
			createRole(config, role)
		}
	}

	//check for roles that exist in the system but are not in config and warn about them
	for name := range availableRoles {
		found := false
		for _, role := range config.Roles {
			if strings.EqualFold(name, role.Name) {
				found = true
				break
			}
		}
		if !found {
			log.Printf("- WARNING: role '%s' exists in the system but is not defined in config.json. Please review manually.", name)
		}
	}

	availableRoles, err = fetchRoles(config)
	if err != nil {
		log.Fatalf("failed to fetch roles: %v", err)
	}
	// Debug: sanity check - fetch roles again and print them
	if config.Debug {
		log.Println("All required roles should now exist:")
		for name, id := range availableRoles {
			log.Printf(" - %s (ID: %s)", name, id)
		}
	}

	if config.Debug {
		log.Println("...")
	}

	/*
	  For each role in config, fetch permissions
	  Verify that permissions match expected permissions for that role in config
	  If not, update permissions to match expected permissions
	  Warn about existing permissions for a role not matching expected permissions
	*/

	for _, role := range config.Roles {
		if config.Debug {
			log.Printf(">> Verifying permissions for role: %s", role.Name)
		}

		roleId, exists := availableRoles[strings.ToLower(role.Name)]
		if !exists {
			log.Fatalf("role '%s' not found in available roles after creation step", role.Name)
		}

		permissions, err := fetchPermissionsForRole(config, roleId)
		if err != nil {
			log.Fatalf("failed to fetch permissions for role '%s': %v", role.Name, err)
		}

		for namespace, perms := range role.Permissions {
			expectedPerms, ok := role.Permissions[namespace]
			if !ok {
				log.Printf("- WARNING: role '%s' has unexpected namespace '%s' with permissions %v. Please review manually.", role.Name, namespace, perms)
				continue
			}
			// if namespace exists in current permissions else empty slice
			existingPerms, okExisting := permissions[namespace]
			if !okExisting {
				existingPerms = []string{}
			}

			extraPermissions, missingPermissions := differences(existingPerms, expectedPerms)
			for _, p := range extraPermissions {
				log.Printf("- WARNING: role '%s' has unexpected permission '%s' in namespace '%s'. Please review manually.", role.Name, p, namespace)
			}
			for _, p := range missingPermissions {
				grantPermission(config, "Role", roleId, namespace, p)
			}
		}
	}

	if config.Debug {
		log.Println("...")
	}

	/*
	  Fetch all groups and check for "CloudEngineers" group
	  If it does not exist, create it
	*/

	if config.Debug {
		log.Println(">> Consolidating groups...")
	}

	availableGroups, err := fetchGroups(config)
	if err != nil {
		log.Fatalf("failed to fetch groups: %v", err)
	}

	if _, exists := availableGroups["CloudEngineers"]; !exists {
		log.Println("Group 'CloudEngineers' does not exist; creating it...")
		createGroup(config, "CloudEngineers")
	}

	availableGroups, err = fetchGroups(config)
	if err != nil {
		log.Fatalf("failed to fetch groups: %v", err)
	}
	// Debug: print all available groups
	if config.Debug {
		log.Println("Now Available groups:")
		for name, id := range availableGroups {
			log.Printf(" - %s (ID: %s)", name, id)
		}
	}

	if config.Debug {
		log.Println("...")
	}

	/*
	  Fetch all role for "CloudEngineers" group
	  Check for Owner Global permission
	  If not found, assign it
	*/
	if config.Debug {
		log.Println(">> Ensuring 'CloudEngineers' group has correct roles...")
	}
	group := availableGroups["CloudEngineers"]
	assignedRoles, err := fetchRoleGrantsForGroup(config, group.ID)
	if err != nil {
		log.Fatalf("failed to fetch role grants for group: %v", err)
	}

	if config.Debug {
		log.Printf("'CloudEngineers' group currently has %d assigned roles.", len(assignedRoles))
		for _, ra := range assignedRoles {
			log.Printf(" - Role ID: %s, Entity Type: %s, Entity ID: %s, Type: %s, Resource: %s", ra.RoleId, ra.AssignedEntityType, ra.AssignedEntityId, ra.Type, ra.Resource)
		}
	}

	// ensure all roles from config are assigned to the group
	for _, r := range config.CloudEngineerRoles {
		roleFound := false
		for _, ra := range assignedRoles {
			if ra.RoleId == availableRoles[strings.ToLower(r.RoleName)] && strings.EqualFold(ra.Type, r.Scope) {
				roleFound = true
				break
			}
		}
		if !roleFound {
			log.Printf("- Assigning %s role to 'CloudEngineers' group...", r.RoleName)
			err := assignRole(config, availableRoles[strings.ToLower(r.RoleName)], "Group", group.ID, r.Scope, "")
			if err != nil {
				log.Fatalf("failed to assign role: %v", err)
			}
			if config.Debug {
				log.Printf("- Assigned %s -- %s role to 'CloudEngineers' group.", r.RoleName, r.Scope)
			}
		} else {
			if config.Debug {
				log.Printf("'CloudEngineers' group already has %s -- %s role; no action needed.", r.RoleName, r.Scope)
			}
		}
	}
	// ensure that no other roles are assigned to the group
	for _, ra := range assignedRoles {
		roleDesired := false
		for _, r := range config.CloudEngineerRoles {
			if ra.RoleId == availableRoles[strings.ToLower(r.RoleName)] && strings.EqualFold(ra.Type, r.Scope) {
				roleDesired = true
				break
			}
		}
		if !roleDesired {
			log.Printf("- WARNING: 'CloudEngineers' group has unexpected role assigned (Role ID: %s, Type: %s, Scope: %s). Please review manually.", ra.RoleId, availableRoles[ra.RoleId], ra.Type)
		}
	}

	if config.Debug {
		log.Println("...")
	}

	/*
	  Fetch all existing members of group "CloudEngineers"
	  For all cloud engineers in config, check if they are already members
	  If not, add them
	*/
	if config.Debug {
		log.Println(">> Ensuring all cloud engineers are members of 'CloudEngineers' group...")
	}
	group = availableGroups["CloudEngineers"]

	missingMembers, unwantedMembers := differences(config.Cloudengineers, extractEmails(group.Members))

	// add missing members
	for _, email := range missingMembers {
		log.Printf("- Adding missing member: %s", email)
		createMembership(config, group.ID, email)
	}
	// remove unwanted members
	for _, email := range unwantedMembers {
		log.Printf("- WARNING: 'CloudEngineers' group has unexpected member: %s. Please review manually.", email)
	}

	if config.Debug {
		log.Println("Final members of 'CloudEngineers' group:")
		availableGroups, err = fetchGroups(config)
		if err != nil {
			log.Fatalf("failed to fetch groups: %v", err)
		}
		group = availableGroups["CloudEngineers"]
		for _, email := range group.Members {
			log.Printf(" - %s", email)
		}
	}

	if config.Debug {
		log.Println("...")
	}

	log.Println("<< Baseline permissions setup completed.")
}

/**
 **
 **
 **
 ** Supporting functions
 **
 **
 **
 **/

/*
Loading configuration
*/

type Role struct {
	Name        string              `json:"name"`
	Permissions map[string][]string `json:"permissions"`
}

type Config struct {
	Debug              bool     `json:"debug"`
	ApiUrl             string   `json:"apiUrl"`
	Cloudengineers     []string `json:"cloudengineers"`
	CloudEngineerRoles []struct {
		RoleName string `json:"roleName"`
		Scope    string `json:"scope"`
	} `json:"cloudengineerRoles"`
	AccessToken string // not from config, set from env var 'SELF_SERVICE_API_TOKEN'
	Roles       []Role `json:"roles"`
}

func loadConfig(path string) (*Config, error) {
	const accessTokenEnvVar = "SELF_SERVICE_API_TOKEN"

	file, err := os.ReadFile(path)
	if err != nil {
		return nil, err
	}

	var cfg Config
	if err := json.Unmarshal(file, &cfg); err != nil {
		return nil, err
	}

	// read token from environment variable
	token := os.Getenv(accessTokenEnvVar)
	if token == "" {
		return nil, fmt.Errorf("environment variable %s is not set", accessTokenEnvVar)
	}
	cfg.AccessToken = token

	return &cfg, nil
}

/*
Data structures for API communication
*/
type Capability struct {
	ID           string `json:"id"`
	Status       string `json:"status"`
	JsonMetadata string `json:"jsonMetadata"`
}

type RoleAssignment struct {
	RoleId             string `json:"roleId"`
	AssignedEntityType string `json:"assignedEntityType"`
	AssignedEntityId   string `json:"assignedEntityId"`
	Type               string `json:"type"`
	Resource           string `json:"resource"`
}

type PermissionGrant struct {
	Namespace  string `json:"namespace"`
	Permission string `json:"permission"`
	Type       string `json:"type"`
	Resource   string `json:"resource"`
}

type PermissionGrantCreation struct {
	Namespace          string `json:"namespace"`
	Permission         string `json:"permission"`
	Type               string `json:"type"`
	Resource           string `json:"resource"`
	AssignedEntityType string `json:"assignedEntityType"`
	AssignedEntityId   string `json:"assignedEntityId"`
}

type SystemRole struct {
	ID   string `json:"id"`
	Name string `json:"name"`
	Type string `json:"type"`
}

type Member struct {
	UserId string `json:"userId"`
	//GroupId string `json:"groupId"`
}

type Group struct {
	ID      string   `json:"id"`
	Name    string   `json:"name"`
	Members []Member `json:"members"`
}

/*
  Functions
*/

func fetchRoles(config *Config) (map[string]string, error) {
	url := fmt.Sprintf("%s/rbac/get-assignable-roles", config.ApiUrl)
	req, _ := http.NewRequest("GET", url, nil)
	req.Header.Set("Authorization", "Bearer "+config.AccessToken)

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		log.Printf("failed to fetch roles: %v", err)
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		log.Printf("unexpected status %d", resp.StatusCode)
		return nil, fmt.Errorf("unexpected status %d", resp.StatusCode)
	}

	var roles []SystemRole
	if err := json.NewDecoder(resp.Body).Decode(&roles); err != nil {
		log.Printf("failed to decode roles response: %v", err)
		return nil, err
	}

	availableRoles := make(map[string]string)
	for _, role := range roles {
		availableRoles[strings.ToLower(role.Name)] = role.ID
	}

	return availableRoles, nil
}

func createRole(config *Config, role Role) {
	url := fmt.Sprintf("%s/rbac/role", config.ApiUrl)

	payload := map[string]string{
		"name":        role.Name,
		"description": fmt.Sprintf("Automatically created role: %s", role.Name),
		"type":        "Global",
	}
	body, _ := json.Marshal(payload)

	req, _ := http.NewRequest("POST", url, bytes.NewBuffer(body))
	req.Header.Set("Authorization", "Bearer "+config.AccessToken)
	req.Header.Set("Content-Type", "application/json")

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		log.Fatalf("failed to create role: %v", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusCreated {
		log.Fatalf("failed to create role, [error code %d]", resp.StatusCode)
	}
}

func fetchGroups(config *Config) (map[string]Group, error) {
	url := fmt.Sprintf("%s/rbac/groups", config.ApiUrl)
	req, _ := http.NewRequest("GET", url, nil)
	req.Header.Set("Authorization", "Bearer "+config.AccessToken)

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		log.Printf("failed to fetch groups: %v", err)
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		log.Printf("unexpected status %d", resp.StatusCode)
		return nil, fmt.Errorf("unexpected status %d", resp.StatusCode)
	}

	var groups []Group
	if err := json.NewDecoder(resp.Body).Decode(&groups); err != nil {
		log.Printf("failed to decode groups response: %v", err)
		return nil, err
	}

	availableGroups := make(map[string]Group)
	for _, group := range groups {
		availableGroups[group.Name] = group
	}

	return availableGroups, nil
}

func createGroup(config *Config, groupName string) {
	url := fmt.Sprintf("%s/rbac/groups", config.ApiUrl)
	payload := map[string]string{
		"name":        groupName,
		"description": fmt.Sprintf("Automatically created group: %s", groupName),
	}
	body, _ := json.Marshal(payload)

	req, _ := http.NewRequest("POST", url, bytes.NewBuffer(body))
	req.Header.Set("Authorization", "Bearer "+config.AccessToken)
	req.Header.Set("Content-Type", "application/json")

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		log.Fatalf("failed to create group: %v", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusCreated {
		log.Fatalf("failed to create group, [error code %d]", resp.StatusCode)
	}
}

func createMembership(config *Config, groupID, email string) {
	url := fmt.Sprintf("%s/rbac/groups/%s/members", config.ApiUrl, groupID)
	payload := map[string]string{
		"userId":  email,
		"groupId": groupID,
	}
	body, _ := json.Marshal(payload)

	req, _ := http.NewRequest("POST", url, bytes.NewBuffer(body))
	req.Header.Set("Authorization", "Bearer "+config.AccessToken)
	req.Header.Set("Content-Type", "application/json")

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		log.Fatalf("failed to create membership: %v", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusCreated {
		log.Fatalf("failed to create membership, [error code %d]", resp.StatusCode)
	}
}

func removeMembership(config *Config, groupID, memberId string) {
	url := fmt.Sprintf("%s/rbac/groups/%s/members/%s", config.ApiUrl, groupID, memberId)
	req, _ := http.NewRequest("DELETE", url, nil)
	req.Header.Set("Authorization", "Bearer "+config.AccessToken)

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		log.Fatalf("failed to remove membership: %v", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusNoContent {
		log.Fatalf("failed to remove membership, [error code %d]", resp.StatusCode)
	}
}

func assignRole(config *Config, roleId, assignedEntityType, assignedEntityId, assignmentType, resource string) error {
	url := fmt.Sprintf("%s/rbac/role/grant", config.ApiUrl)

	payload := RoleAssignment{
		RoleId:             roleId,
		AssignedEntityType: assignedEntityType,
		AssignedEntityId:   assignedEntityId,
		Type:               assignmentType,
		Resource:           resource,
	}
	body, _ := json.Marshal(payload)

	req, _ := http.NewRequest("POST", url, bytes.NewBuffer(body))
	req.Header.Set("Authorization", "Bearer "+config.AccessToken)
	req.Header.Set("Content-Type", "application/json")

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		log.Printf("failed to assign role: %v", err)
		return err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusNoContent {
		b, _ := ioutil.ReadAll(resp.Body)
		return fmt.Errorf("failed to assign role: %s, [error code %d]", string(b), resp.StatusCode)
	}

	return nil
}

func fetchRoleGrantsForGroup(config *Config, groupID string) ([]RoleAssignment, error) {
	url := fmt.Sprintf("%s/rbac/role/groups/%s", config.ApiUrl, groupID)
	req, _ := http.NewRequest("GET", url, nil)
	req.Header.Set("Authorization", "Bearer "+config.AccessToken)

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		log.Printf("failed to fetch role grants for group: %v", err)
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		log.Printf("unexpected status %d", resp.StatusCode)
		return nil, fmt.Errorf("unexpected status %d", resp.StatusCode)
	}

	var roleAssignments []RoleAssignment
	if err := json.NewDecoder(resp.Body).Decode(&roleAssignments); err != nil {
		log.Printf("failed to decode role grants response: %v", err)
		return nil, err
	}

	return roleAssignments, nil
}

func fetchPermissionsForRole(config *Config, roleId string) (map[string][]string, error) {
	url := fmt.Sprintf("%s/rbac/permission/role/%s", config.ApiUrl, roleId)
	req, _ := http.NewRequest("GET", url, nil)
	req.Header.Set("Authorization", "Bearer "+config.AccessToken)

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		log.Printf("failed to fetch permissions for role: %v", err)
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		log.Printf("unexpected status %d", resp.StatusCode)
		return nil, fmt.Errorf("unexpected status %d", resp.StatusCode)
	}

	var permissions []PermissionGrant
	if err := json.NewDecoder(resp.Body).Decode(&permissions); err != nil {
		log.Printf("failed to decode permissions response: %v", err)
		return nil, err
	}

	permissionMap := make(map[string][]string)
	for _, p := range permissions {
		permissionMap[p.Namespace] = append(permissionMap[p.Namespace], p.Permission)
	}

	return permissionMap, nil
}

func grantPermission(config *Config, entityType, entityId, namespace, permission string) {
	url := fmt.Sprintf("%s/rbac/permission/grant", config.ApiUrl)

	payload := PermissionGrantCreation{
		Namespace:          namespace,
		Permission:         permission,
		AssignedEntityType: entityType,
		AssignedEntityId:   entityId,
		Type:               "Global",
		Resource:           "*",
	}
	body, _ := json.Marshal(payload)

	req, _ := http.NewRequest("POST", url, bytes.NewBuffer(body))
	req.Header.Set("Authorization", "Bearer "+config.AccessToken)
	req.Header.Set("Content-Type", "application/json")

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		log.Fatalf("failed to grant permission: %v", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusCreated && resp.StatusCode != http.StatusNoContent {
		log.Fatalf("failed to grant permission, [error code %d]", resp.StatusCode)
	}

	if config.Debug {
		log.Printf("- Granted missing permission '%s' in namespace '%s' to %s (%s).", permission, namespace, entityType, entityId)
	}
}

func differences(a, b []string) (onlyInA, onlyInB []string) {
	setA := make(map[string]struct{}, len(a))
	setB := make(map[string]struct{}, len(b))

	for _, v := range a {
		setA[v] = struct{}{}
	}
	for _, v := range b {
		setB[v] = struct{}{}
	}

	// elements in A but not in B
	for _, v := range a {
		if _, found := setB[v]; !found {
			onlyInA = append(onlyInA, v)
		}
	}
	// elements in B but not in A
	for _, v := range b {
		if _, found := setA[v]; !found {
			onlyInB = append(onlyInB, v)
		}
	}

	return
}

func extractEmails(members []Member) []string {
	out := make([]string, 0, len(members))
	for _, m := range members {
		out = append(out, m.UserId)
	}
	return out
}
