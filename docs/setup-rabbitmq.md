# RabbitMQ Installation Guide

This guide provides detailed instructions for installing and configuring RabbitMQ across various environments.

## 1. Standalone Installation

### Prerequisites

Before installing RabbitMQ, ensure you have Erlang installed on your system.

#### Windows

1. Install Erlang:
   ```powershell
   # Using Chocolatey
   choco install erlang

   # Verify installation
   erl -version
   ```

2. Install RabbitMQ:
   ```powershell
   # Using Chocolatey
   choco install rabbitmq

   # Start the service
   net start RabbitMQ
   ```

3. Enable Management Plugin:
   ```powershell
   rabbitmq-plugins enable rabbitmq_management
   ```

#### Linux (Ubuntu)

1. Install Erlang:
   ```bash
   # Add Erlang repository
   curl -fsSL https://packages.erlang-solutions.com/ubuntu/erlang_solutions.asc | sudo apt-key add -
   echo "deb https://packages.erlang-solutions.com/ubuntu $(lsb_release -cs) contrib" | sudo tee /etc/apt/sources.list.d/erlang.list

   # Install Erlang
   sudo apt update
   sudo apt install -y erlang
   ```

2. Install RabbitMQ:
   ```bash
   # Add RabbitMQ repository
   curl -s https://packagecloud.io/install/repositories/rabbitmq/rabbitmq-server/script.deb.sh | sudo bash
   
   # Install RabbitMQ
   sudo apt install -y rabbitmq-server

   # Start the service
   sudo systemctl start rabbitmq-server
   sudo systemctl enable rabbitmq-server
   ```

3. Enable Management Plugin:
   ```bash
   sudo rabbitmq-plugins enable rabbitmq_management
   ```

#### macOS

1. Install using Homebrew:
   ```bash
   # Install Erlang and RabbitMQ
   brew install erlang
   brew install rabbitmq

   # Start the service
   brew services start rabbitmq
   ```

2. Enable Management Plugin:
   ```bash
   /usr/local/sbin/rabbitmq-plugins enable rabbitmq_management
   ```

### Accessing Management UI

After installation, access the management UI at: http://localhost:15672

Default credentials:
- Username: guest
- Password: guest

## 2. Docker Setup

### Create Network
First, create a dedicated network for RabbitMQ and related services:

```bash
# Create a user-defined network
docker network create rabbitmq-net
```

### Run RabbitMQ Container
Run RabbitMQ with management plugin using Docker:

```bash
docker run -d --name rabbitmq \
    --network rabbitmq-net \  # Attach to the custom network
    -p 5672:5672 \           # AMQP port
    -p 15672:15672 \         # Management UI port
    -p 5671:5671 \           # AMQP with TLS port
    -p 15692:15692 \         # Prometheus metrics port
    -v rabbitmq_data:/var/lib/rabbitmq \  # Persist data
    -e RABBITMQ_DEFAULT_USER=admin \      # Custom username
    -e RABBITMQ_DEFAULT_PASS=secret123 \  # Custom password
    -e RABBITMQ_ERLANG_COOKIE=unique-cookie-value \  # For clustering
    --hostname rabbitmq-1 \   # Set hostname for clustering
    --health-cmd "rabbitmq-diagnostics check_running" \  # Health check
    --health-interval=30s \
    --health-timeout=10s \
    --health-retries=5 \
    rabbitmq:3.13-management
```

### Network Usage
Other containers can now connect to RabbitMQ using the container name as hostname:
```bash
# Example: Running a service that needs to connect to RabbitMQ
docker run -d --name my-service \
    --network rabbitmq-net \
    -e RABBITMQ_HOST=rabbitmq \  # Use container name as hostname
    my-service-image
```

## 3. Docker Compose Setup

Create a `docker-compose.yml` file:

```yaml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:3.13-management
    container_name: rabbitmq
    ports:
      - "5672:5672"    # AMQP port
      - "15672:15672"  # Management UI port
    environment:
      - RABBITMQ_DEFAULT_USER=${RABBITMQ_USER:-admin}      # Default: admin
      - RABBITMQ_DEFAULT_PASS=${RABBITMQ_PASS:-secret123}  # Default: secret123
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq  # Persist data
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "check_port_connectivity"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  rabbitmq_data:  # Named volume for persistence
```

Start and stop the service:
```bash
# Start RabbitMQ
docker-compose up -d

# Stop RabbitMQ
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

## 4. Kubernetes with Helm

Deploy RabbitMQ using the Bitnami Helm chart:

1. Add Bitnami repository:
   ```bash
   helm repo add bitnami https://charts.bitnami.com/bitnami
   helm repo update
   ```

2. Create `values.yaml`:
   ```yaml
   # RabbitMQ Helm values
   auth:
     username: admin
     password: secret123
     erlangCookie: "RABBITMQ-ERLANG-COOKIE"

   persistence:
     enabled: true
     storageClass: "standard"  # Adjust to your cluster's storage class
     size: 8Gi

   replicaCount: 3  # High availability setup

   resources:
     requests:
       memory: "256Mi"
       cpu: "250m"
     limits:
       memory: "512Mi"
       cpu: "500m"

   service:
     type: ClusterIP

   metrics:
     enabled: true  # Enable Prometheus metrics
   ```

3. Install RabbitMQ:
   ```bash
   # Create namespace
   kubectl create namespace rabbitmq

   # Install RabbitMQ
   helm install rabbitmq bitnami/rabbitmq \
     --namespace rabbitmq \
     --values values.yaml
   ```

4. Access Management UI:
   ```bash
   # Port forward the management UI
   kubectl port-forward --namespace rabbitmq svc/rabbitmq 15672:15672
   ```

  ## 4a. Kubernetes: RabbitMQ Cluster Operator

  Deploy RabbitMQ using the official RabbitMQ Cluster Operator for advanced clustering and lifecycle management.

  ### 1. Install the RabbitMQ Cluster Operator (Helm)
  ```bash
  # Add the RabbitMQ Helm repo
  helm repo add rabbitmq https://charts.rabbitmq.com
  helm repo update

  # Create namespace for operator and clusters
  kubectl create namespace rabbitmq-system

  # Install the operator
  helm install rabbitmq-cluster-operator rabbitmq/rabbitmq-cluster-operator \
    --namespace rabbitmq-system
  ```

  ### 2. Deploy a RabbitMQ Cluster
  Create a `rabbitmq-cluster.yaml` manifest:
  ```yaml
  apiVersion: rabbitmq.com/v1beta1
  kind: RabbitmqCluster
  metadata:
    name: my-rabbitmq
    namespace: rabbitmq
  spec:
    replicas: 3  # Number of nodes in the cluster
    resources:
      requests:
        memory: "256Mi"
        cpu: "250m"
      limits:
        memory: "512Mi"
        cpu: "500m"
    persistence:
      storageClassName: "standard"  # Adjust to your cluster
      storage: "8Gi"
    rabbitmq:
      additionalConfig: |
        management.load_definitions = /etc/rabbitmq/definitions.json
    image: "rabbitmq:3.13-management"  # Use management image for UI
    service:
      type: ClusterIP
    override:
      service:
        ports:
          - name: amqp
            port: 5672
          - name: management
            port: 15672
  ```
  Apply the manifest:
  ```bash
  kubectl create namespace rabbitmq
  kubectl apply -f rabbitmq-cluster.yaml
  ```

  ### 3. Access Management UI
  ```bash
  # Port forward the management UI
  kubectl port-forward --namespace rabbitmq svc/my-rabbitmq 15672:15672
  ```

  ### 4. Credentials
  Get the default credentials:
  ```bash
  kubectl get secret my-rabbitmq-default-user -n rabbitmq -o jsonpath='{.data.username}' | base64 -d
  kubectl get secret my-rabbitmq-default-user -n rabbitmq -o jsonpath='{.data.password}' | base64 -d
  ```

  ### 5. Notes & Best Practices
  - The Cluster Operator automates scaling, upgrades, and recovery.
  - Use custom resource fields to configure advanced clustering, TLS, and monitoring.
  - Always change default credentials and enable TLS for production.
## 5. Terraform Deployment

Create a Terraform configuration for deploying RabbitMQ on Kubernetes:

```hcl
# providers.tf
terraform {
  required_providers {
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.25.0"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.12.0"
    }
  }
}

provider "kubernetes" {
  config_path = "~/.kube/config"  # Adjust path as needed
}

provider "helm" {
  kubernetes {
    config_path = "~/.kube/config"  # Adjust path as needed
  }
}

# main.tf
resource "kubernetes_namespace" "rabbitmq" {
  metadata {
    name = "rabbitmq"
  }
}

resource "helm_release" "rabbitmq" {
  name       = "rabbitmq"
  repository = "https://charts.bitnami.com/bitnami"
  chart      = "rabbitmq"
  namespace  = kubernetes_namespace.rabbitmq.metadata[0].name
  version    = "12.6.1"  # Specify the chart version

  values = [
    file("${path.module}/values.yaml")
  ]

  # Override specific values
  set {
    name  = "auth.username"
    value = "admin"
  }

  set {
    name  = "auth.password"
    value = "secret123"
  }

  set {
    name  = "replicaCount"
    value = "3"
  }
}

# outputs.tf
output "rabbitmq_service" {
  value = "${helm_release.rabbitmq.name}-rabbitmq.${helm_release.rabbitmq.namespace}.svc.cluster.local"
}

output "management_url" {
  value = "http://localhost:15672 (after port-forwarding)"
}
```

Deploy using Terraform:
```bash
# Initialize Terraform
terraform init

# Plan the deployment
terraform plan

# Apply the configuration
terraform apply

# Destroy when done
terraform destroy
```

## Additional Notes

1. **Security Considerations**:
   - Always change default credentials in production
   - Use SSL/TLS for production deployments
   - Implement proper network policies in Kubernetes

2. **Monitoring**:
   - Enable Prometheus metrics when available
   - Set up proper resource monitoring
   - Configure alerts for critical metrics

3. **Backup**:
   - Implement regular backup procedures
   - Test backup restoration regularly
   - Document backup and restore procedures

4. **Maintenance**:
   - Keep RabbitMQ and Erlang versions up to date
   - Schedule regular maintenance windows
   - Monitor system resources and scale as needed





   GitHub Prompt:
   ```
        # Prompt for GitHub Copilot

        Create a detailed step-by-step guide for installing and configuring **RabbitMQ** in multiple environments:

        ## 1. Standalone Installation
        - Include instructions for **Windows**, **Linux (Ubuntu)**, and **macOS**.
        - Cover prerequisites: Erlang installation, RabbitMQ package source, and service setup.
        - Include basic management setup: enabling the management plugin and accessing the RabbitMQ dashboard.

        ## 2. Docker Setup
        - Provide a `docker run` command for a single RabbitMQ instance with the management UI.
        - Explain ports, default credentials, and volume mapping for data persistence.

        ## 3. Docker Compose
        - Create a `docker-compose.yml` file defining:
        - RabbitMQ service with management plugin
        - Persistent volumes
        - Custom user/password environment variables
        - Include instructions to start and stop the service.

        ## 4. Kubernetes with Helm
        - Use the **official Bitnami RabbitMQ Helm chart**.
        - Provide commands to:
        - Add the Bitnami repo
        - Install RabbitMQ in a namespace
        - Expose the management UI using `kubectl port-forward`
        - Include values.yaml customization (username, password, storage class, replica count).

        ## 5. Terraform Deployment
        - Show how to deploy RabbitMQ on a Kubernetes cluster using Terraform.
        - Include:
        - Provider configuration (Helm + Kubernetes)
        - Terraform HCL snippet to deploy the RabbitMQ Helm release
        - Output section to display access credentials or URLs.

        ## Additional Requirements
        - Use code blocks for all shell commands, YAML, and HCL examples.
        - Keep steps minimal, clear, and beginner-friendly.
        - Add comments in code snippets explaining key configurations.
        - Ensure all commands are up to date and compatible with RabbitMQ 3.13+ and Helm 3.

        Goal: Produce a single markdown or documentation file that walks through RabbitMQ installation across all these environments.

    ```