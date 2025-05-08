# Project Overview

## 1. Calculation Challenge

The project is designed to efficiently calculate emissions data using **asynchronous programming** and **parallel programming** techniques. By leveraging modern programming paradigms, we ensure:

* High-performance data processing.
* Efficient handling of independent tasks.
* Optimal utilization of system resources.

## 2. Chaos Challenge

To handle API reliability issues such as timeouts and failures, the project utilizes the **Polly** library. Polly provides resilience strategies like:

* Retry policies for transient failures.
* Timeout policies to prevent long waits on delayed responses.

This ensures robustness and reliability under chaotic conditions.

## 3. Docker Challenge

The project includes a fully configured **Docker Compose** setup to simplify deployment. Key highlights:

* Fixed and optimized Dockerfiles for all microservices.
* Configured networking for seamless inter-service communication.
* The entire application can be launched with a single command:

  ```bash
  docker-compose up
  ```

This setup ensures that the project is portable and easy to deploy across various environments.

