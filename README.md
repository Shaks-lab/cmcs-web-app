# Contract Monthly Claim System (CMCS)

## Overview

The Contract Monthly Claim System (CMCS) is a web-based application developed using ASP.NET Core MVC that allows employees to submit, track, and manage monthly claims in an efficient and transparent manner. The system provides a centralized platform for claim submission, document uploads, and claim approval workflows, helping reduce manual administrative work.

The application uses the Model–View–Controller (MVC) architectural pattern and integrates with a database through Entity Framework Core to manage users, claims, and related records.

## Features

### Employee

* Submit monthly claims
* Upload supporting documents
* View previously submitted claims
* Track claim approval status

### HR Staff

* Review employee claims
* Verify supporting documentation
* Approve or reject claims
* Monitor claim activity

### Administrator

* Manage users and roles
* Oversee the entire claim system
* Access system-wide records
* Maintain system settings

## Technologies Used

* ASP.NET Core MVC
* C#
* Entity Framework Core
* SQL Database
* Razor Views
* Bootstrap

## System Architecture

The project follows the MVC architecture:

* **Models**: Represent the application data structures such as claims and users.
* **Views**: Handle the user interface and display data.
* **Controllers**: Manage application logic and user requests.

This separation improves maintainability, scalability, and code organization.

## Installation

1. Clone the repository:

```
git clone https://github.com/Shaks-lab/cmcs-web-app
```

2. Open the project in Visual Studio.

3. Restore NuGet packages.

4. Update the database connection string in `appsettings.json`.

5. Apply database migrations:

```
Update-Database
```

6. Run the application.

## Usage

1. Register or log in to the system.
2. Employees can submit claims through the claim submission page.
3. Upload any required supporting documents.
4. HR staff review claims and approve or reject them.
5. Administrators manage users and oversee system operations.

## Project Purpose

This project was developed as an academic software development project to demonstrate:

* Implementation of a full-stack web application
* Use of ASP.NET Core MVC framework
* Role-based access control
* Database integration using Entity Framework Core
* Real-world workflow automation

## Future Improvements

* Email notifications for claim updates
* Advanced reporting and analytics
* Improved dashboard and UI
* Integration with payroll systems

## License

This project is intended for educational purposes.
