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

Run RabbitMQ with management plugin using Docker:

```bash
docker run -d --name rabbitmq \
    -p 5672:5672 \        # AMQP port
    -p 15672:15672 \      # Management UI port
    -v rabbitmq_data:/var/lib/rabbitmq \  # Persist data
    -e RABBITMQ_DEFAULT_USER=admin \      # Custom username
    -e RABBITMQ_DEFAULT_PASS=secret123 \  # Custom password
    rabbitmq:3.13-management
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
