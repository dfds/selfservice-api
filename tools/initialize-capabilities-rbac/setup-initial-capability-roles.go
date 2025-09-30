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

const (
	apiBaseURL = "http://localhost:8080" // replace with real API base
)

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

type SystemRole struct {
	ID   string `json:"id"`
	Name string `json:"name"`
	Type string `json:"type"`
}

type Member struct {
	Id    string `json:"id"`
	Email string `json:"email"`
}

func main() {
	// Load config
	config, err := loadConfig("config.json")
	if err != nil {
		log.Fatalf("failed to load config: %v", err)
	}

	accessToken := config.AccessToken
	if accessToken == "" {
		log.Fatal("access token is required")
	}

	availableRoles, err := fetchRoles(accessToken)
	if err != nil {
		log.Fatalf("failed to fetch roles: %v", err)
	}

	// Assert that required roles exist
	for _, role := range config.RequiredRoles {
		if _, exists := availableRoles[role]; !exists {
			log.Fatalf("required role %s not found in available roles", role)
		}
	}

	capabilities, err := fetchCapabilities(accessToken)
	if err != nil {
		log.Fatalf("failed to fetch capabilities: %v", err)
	}

	for _, c := range capabilities {
		fmt.Printf("Processing capability: %s\n", c.ID)

		// check for deleted using Status in lower case
		if strings.ToLower(c.Status) == "deleted" {
			continue
		}

		// Fetch members for the capability
		members, err := fetchMembers(accessToken, c.ID)
		if err != nil {
			log.Printf("failed to fetch members for capability %s: %v", c.ID, err)
			continue
		}

		// Metadata is a json string, parse to find dfds.owner
		var metadata map[string]interface{}
		if err := json.Unmarshal([]byte(c.JsonMetadata), &metadata); err != nil {
			log.Printf("failed to parse metadata for capability %s: %v", c.ID, err)
			continue
		}

		// Check if dfds.owner exists and is non-empty
		// if so, set their role to Owner
		ownerEmail, hasOwner := metadata["dfds.owner"].(string)
		if hasOwner && ownerEmail != "" {
			// Grant Contributor to all members
			for _, m := range members {
				err := assignRole(accessToken, c.ID, m.Id, "contributor", availableRoles)
				if err != nil {
					panic(err)
				}
			}

			// Grant Owner to specified owner
			err = assignRole(accessToken, c.ID, ownerEmail, "owner", availableRoles)
			if err != nil {
				panic(err)
			}
		} else {
			// No specified owner, set all members to owner
			for _, m := range members {
				err := assignRole(accessToken, c.ID, m.Id, "owner", availableRoles)
				if err != nil {
					panic(err)
				}
			}
		}
	}
}

type Config struct {
	Debug         bool     `json:"debug"`
	ApiUrl        string   `json:"apiUrl"`
	AccessToken   string   // not from config, set from env var 'SELF_SERVICE_API_TOKEN'
	RequiredRoles []string `json:"requiredRoles"`
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

func fetchRoles(token string) (map[string]string, error) {
	url := fmt.Sprintf("%s/rbac/get-assignable-roles", apiBaseURL)
	req, _ := http.NewRequest("GET", url, nil)
	req.Header.Set("Authorization", "Bearer "+token)

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

func fetchCapabilities(token string) ([]Capability, error) {
	url := fmt.Sprintf("%s/capabilities", apiBaseURL)
	req, _ := http.NewRequest("GET", url, nil)
	req.Header.Set("Authorization", "Bearer "+token)

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := ioutil.ReadAll(resp.Body)
		return nil, fmt.Errorf("unexpected status %d: %s", resp.StatusCode, string(body))
	}

	// get "Items" from response as Capabilities
	var result struct {
		Capabilities []Capability `json:"Items"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return nil, err
	}

	return result.Capabilities, nil
}

func fetchMembers(token, capabilityId string) ([]Member, error) {
	url := fmt.Sprintf("%s/capabilities/%s/members", apiBaseURL, capabilityId)
	req, _ := http.NewRequest("GET", url, nil)

	req.Header.Set("Authorization", "Bearer "+token)
	req.Header.Set("Content-Type", "application/json")

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("unexpected status for %s: %d", url, resp.StatusCode)
	}

	// get "Items" from response as Capabilities
	var result struct {
		Members []Member `json:"Items"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return nil, err
	}

	return result.Members, nil
}

func assignRole(token, capabilityId, email, role string, availableRoles map[string]string) error {
	url := fmt.Sprintf("%s/rbac/role/grant", apiBaseURL)

	payload := RoleAssignment{
		RoleId:             availableRoles[role],
		AssignedEntityType: "User",
		AssignedEntityId:   email,
		Type:               "Capability",
		Resource:           capabilityId,
	}
	body, _ := json.Marshal(payload)

	req, _ := http.NewRequest("POST", url, bytes.NewBuffer(body))
	req.Header.Set("Authorization", "Bearer "+token)
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
