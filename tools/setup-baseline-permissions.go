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
		if shouldSkipRole(role.Name) {
			if config.Debug {
				log.Printf("Skipping role '%s' in baseline sync (managed implicitly by API semantics).", role.Name)
			}
			continue
		}
		if _, exists := availableRoles[strings.ToLower(role.Name)]; !exists {
			log.Printf("required role '%s' does not exist in the system; creating it...", role)
			createRole(config, role)
		}
	}

	//check for roles that exist in the system but are not in config and warn about them
	for name := range availableRoles {
		found := false
		for _, role := range config.Roles {
			if shouldSkipRole(role.Name) {
				continue
			}
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
		if shouldSkipRole(role.Name) {
			if config.Debug {
				log.Printf(">> Skipping permissions verification for role: %s", role.Name)
			}
			continue
		}

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

		normalizedExpected := normalizePermissionMap(role.Permissions)
		normalizedExisting := normalizePermissionMap(permissions)

		for namespace, existingPerms := range normalizedExisting {
			if _, ok := normalizedExpected[namespace]; !ok {
				log.Printf("- WARNING: role '%s' has unexpected namespace '%s' with permissions %v. Please review manually.", role.Name, namespace, existingPerms)
			}
		}

		for namespace, expectedPerms := range normalizedExpected {
			existingPerms := normalizedExisting[namespace]
			extraPermissions, missingPermissions := differences(existingPerms, expectedPerms)
			for _, p := range extraPermissions {
				log.Printf("- WARNING: role '%s' has unexpected permission '%s' in namespace '%s'. Please review manually.", role.Name, p, namespace)
			}
			for _, p := range missingPermissions {
				if err := grantPermission(config, "Role", roleId, namespace, p); err != nil {
					log.Fatalf(
						"failed to grant missing permission for role '%s' (roleId='%s', namespace='%s', permission='%s'): %v",
						role.Name,
						roleId,
						namespace,
						p,
						err,
					)
				}
			}
		}
	}

	if config.Debug {
		log.Println("...")
	}

	if config.Debug {
		log.Println(">> Consolidating groups...")
	}

	managedGroups := resolveManagedGroups(config)

	availableGroups, err := fetchGroups(config)
	if err != nil {
		log.Fatalf("failed to fetch groups: %v", err)
	}

	for _, groupSpec := range managedGroups {
		if _, exists := availableGroups[groupSpec.Name]; !exists {
			log.Printf("Group '%s' does not exist; creating it...", groupSpec.Name)
			createGroup(config, groupSpec.Name)
		}
	}

	availableGroups, err = fetchGroups(config)
	if err != nil {
		log.Fatalf("failed to fetch groups: %v", err)
	}

	if config.Debug {
		log.Println("Now Available groups:")
		for name, group := range availableGroups {
			log.Printf(" - %s (ID: %s)", name, group.ID)
		}
	}

	for _, groupSpec := range managedGroups {
		ensureGroupRoles(config, groupSpec, availableGroups, availableRoles)
		//ensureGroupMembers(config, groupSpec, availableGroups)
		log.Printf("Skipping member synchronization for group '%s' (members are managed manually).", groupSpec.Name)
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

type RoleBinding struct {
	RoleName string `json:"roleName"`
	Scope    string `json:"scope"`
}

type ManagedGroup struct {
	Name    string
	Roles   []RoleBinding
	Members []string
}

type ManagedGroupConfig struct {
	Name    string        `json:"name"`
	Roles   []RoleBinding `json:"roles"`
	Members []string      `json:"members"`
}

type Config struct {
	Debug                   bool          `json:"debug"`
	ApiUrl                  string        `json:"apiUrl"`
	Groups                  []ManagedGroupConfig `json:"groups"`
	Cloudengineers          []string      `json:"cloudengineers"`
	BatchCapabilityCreators []string      `json:"batchCapabilityCreators"`
	ServiceCatalogueReaders []string      `json:"serviceCatalogueReaders"`
	CloudEngineerRoles      []RoleBinding `json:"cloudengineerRoles"`
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

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusCreated && resp.StatusCode != http.StatusNoContent {
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

func ensureGroupRoles(config *Config, groupSpec ManagedGroup, availableGroups map[string]Group, availableRoles map[string]string) {
	group, exists := availableGroups[groupSpec.Name]
	if !exists {
		log.Fatalf("group '%s' is not available for role synchronization", groupSpec.Name)
	}

	roleAssignments, err := fetchRoleGrantsForGroup(config, group.ID)
	if err != nil {
		log.Fatalf("failed to fetch role grants for group '%s': %v", groupSpec.Name, err)
	}

	existingRoleGrants := make(map[string]RoleAssignment)
	for _, assignment := range roleAssignments {
		key := fmt.Sprintf(
			"%s|%s|%s",
			assignment.RoleId,
			strings.ToLower(strings.TrimSpace(assignment.Type)),
			normalizeGrantResource(assignment.Type, assignment.Resource),
		)
		existingRoleGrants[key] = assignment
	}

	expectedRoleGrants := make(map[string]RoleBinding)
	for _, binding := range groupSpec.Roles {
		roleID, roleExists := availableRoles[strings.ToLower(binding.RoleName)]
		if !roleExists {
			log.Fatalf("role '%s' required for group '%s' does not exist", binding.RoleName, groupSpec.Name)
		}

		assignmentType := strings.TrimSpace(binding.Scope)
		if assignmentType == "" {
			assignmentType = "Global"
		}

		key := fmt.Sprintf(
			"%s|%s|%s",
			roleID,
			strings.ToLower(assignmentType),
			normalizeGrantResource(assignmentType, "*"),
		)
		expectedRoleGrants[key] = binding

		if _, granted := existingRoleGrants[key]; !granted {
			resource := normalizeGrantResource(assignmentType, "*")
			if err := assignRole(config, roleID, "Group", group.ID, assignmentType, resource); err != nil {
				log.Fatalf("failed to assign role '%s' to group '%s': %v", binding.RoleName, groupSpec.Name, err)
			}
			if config.Debug {
				log.Printf("- Assigned role '%s' (%s) to group '%s'.", binding.RoleName, assignmentType, groupSpec.Name)
			}
		}
	}

	for key, assignment := range existingRoleGrants {
		if _, expected := expectedRoleGrants[key]; !expected {
			log.Printf("- WARNING: group '%s' has unexpected role assignment (roleId='%s', type='%s', resource='%s'). Please review manually.", groupSpec.Name, assignment.RoleId, assignment.Type, assignment.Resource)
		}
	}
}

func resolveManagedGroups(config *Config) []ManagedGroup {
	if len(config.Groups) > 0 {
		groups := make([]ManagedGroup, 0, len(config.Groups))
		for _, g := range config.Groups {
			groups = append(groups, ManagedGroup{Name: g.Name, Roles: g.Roles, Members: g.Members})
		}
		return groups
	}

	return []ManagedGroup{
		{
			Name:    "CloudEngineers",
			Roles:   config.CloudEngineerRoles,
			Members: config.Cloudengineers,
		},
		{
			Name: "BatchCapabilityCreators",
			Roles: []RoleBinding{
				{RoleName: "BatchCapabilityCreator", Scope: "Global"},
			},
			Members: config.BatchCapabilityCreators,
		},
		{
			Name: "ServiceCatalogueReaders",
			Roles: []RoleBinding{
				{RoleName: "ServiceCatalogueReader", Scope: "Global"},
			},
			Members: config.ServiceCatalogueReaders,
		},
	}
}

func normalizeGrantResource(scope, resource string) string {
	if strings.EqualFold(strings.TrimSpace(scope), "Global") {
		return ""
	}
	return strings.TrimSpace(resource)
}

func ensureGroupMembers(config *Config, groupSpec ManagedGroup, availableGroups map[string]Group) {
	group, exists := availableGroups[groupSpec.Name]
	if !exists {
		log.Fatalf("group '%s' is not available for member synchronization", groupSpec.Name)
	}

	existingMembers := make(map[string]string)
	for _, member := range group.Members {
		normalized := strings.ToLower(strings.TrimSpace(member.UserId))
		if normalized != "" {
			existingMembers[normalized] = member.UserId
		}
	}

	expectedMembers := make(map[string]string)
	for _, member := range groupSpec.Members {
		normalized := strings.ToLower(strings.TrimSpace(member))
		if normalized != "" {
			expectedMembers[normalized] = member
		}
	}

	for normalized, member := range expectedMembers {
		if _, exists := existingMembers[normalized]; !exists {
			createMembership(config, group.ID, member)
			if config.Debug {
				log.Printf("- Added member '%s' to group '%s'.", member, groupSpec.Name)
			}
		}
	}

	for normalized, member := range existingMembers {
		if _, expected := expectedMembers[normalized]; !expected {
			removeMembership(config, group.ID, member)
			if config.Debug {
				log.Printf("- Removed member '%s' from group '%s'.", member, groupSpec.Name)
			}
		}
	}
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
		// Baseline script manages global role permissions only.
		if !strings.EqualFold(p.Type, "Global") {
			continue
		}
		permissionMap[p.Namespace] = append(permissionMap[p.Namespace], p.Permission)
	}

	return permissionMap, nil
}

func shouldSkipRole(roleName string) bool {
	return strings.EqualFold(strings.TrimSpace(roleName), "Guest")
}

func normalizePermissionMap(input map[string][]string) map[string][]string {
	output := make(map[string][]string, len(input))
	for namespace, permissions := range input {
		ns := strings.TrimSpace(strings.ToLower(namespace))
		if ns == "" {
			continue
		}

		seen := map[string]struct{}{}
		normalized := make([]string, 0, len(permissions))
		for _, permission := range permissions {
			p := strings.TrimSpace(strings.ToLower(permission))
			if p == "" {
				continue
			}
			if _, exists := seen[p]; exists {
				continue
			}
			seen[p] = struct{}{}
			normalized = append(normalized, p)
		}

		output[ns] = normalized
	}
	return output
}

func grantPermission(config *Config, entityType, entityId, namespace, permission string) error {
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
	if config.Debug {
		log.Printf(
			">> Granting missing permission: entityType='%s', entityId='%s', namespace='%s', permission='%s', type='%s', resource='%s'",
			entityType,
			entityId,
			namespace,
			permission,
			payload.Type,
			payload.Resource,
		)
	}

	req, _ := http.NewRequest("POST", url, bytes.NewBuffer(body))
	req.Header.Set("Authorization", "Bearer "+config.AccessToken)
	req.Header.Set("Content-Type", "application/json")

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		return err
	}
	defer resp.Body.Close()
	responseBody, _ := ioutil.ReadAll(resp.Body)

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusCreated && resp.StatusCode != http.StatusNoContent {
		return fmt.Errorf("status=%d response=%q", resp.StatusCode, string(responseBody))
	}

	if config.Debug {
		log.Printf("- Granted missing permission '%s' in namespace '%s' to %s (%s).", permission, namespace, entityType, entityId)
	}

	return nil
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
