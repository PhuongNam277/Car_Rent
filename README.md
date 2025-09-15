# Car Rent Website

## Introduction
Car Rent Website is a full-stack web application built with **.NET 8** that provides a modern solution for car rental services.  
It allows customers to **browse, reserve, and review cars**, while providing an **admin dashboard** for managing users, cars, reservations, and payments.  
The project is designed to be **scalable, user-friendly, and secure**.

---

## Features
- **Authentication & Authorization**  
  + Cookie-based authentication, Claims, Custom Roles (Admin, Staff, User)  
  + Secure password hashing with **BCrypt**

- **Car & Reservation Management**  
  - CRUD operations for cars, categories, and reservations  
  - Advanced search, filtering, sorting, and pagination  
  - Reservation workflow: Pending → Confirmed → Completed/Cancelled

- **Realtime Chat**  
  - Live chat between customers and staff using **SignalR**  
  - Unread message notifications and real-time updates

- **Payments & Reviews**  
  - Manage payments, invoices, reviews, and comments from customers

- **Admin Dashboard**  
  - Responsive UI built with **Razor, Bootstrap, jQuery**  
  - TagHelpers for reusable components (search, sorting, pagination)

---

## Tech Stack
- **Backend**: ASP.NET Core MVC 8, Entity Framework Core  
- **Frontend**: Razor Pages, Bootstrap, JavaScript (jQuery)  
- **Database**: SQL Server (CarRentalDB), EF Core Migrations  
- **Security**: CookieAuth, Claims, BCrypt password hashing  
- **Realtime**: SignalR  
- **DevOps / Deployment**: Docker
- **Version Control**: Git/GitHub  

---

## Database Design
- **Entities**: Users, Cars, Categories, Reservations, Payments, Reviews, Comments, Roles, Chat Messages, Conversations, etc.  
- Relationships designed with **Entity Framework Core**  
- Normalized schema with LINQ-based queries

---

