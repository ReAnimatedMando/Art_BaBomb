Art Ba-Bomb
Film & Television Art Department Workflow Management
Art Ba-Bomb is a professional project and shopping workflow application built specifically for Film, Television, and Commercial Art Crews.
Designed with real production workflows in mind, the platform helps teams organize projects, track purchases, manage returns, monitor budgets,
and streamline collaboration between production leadership and shoppers.

Built using ASP.NET Core MVC, MySQL, and Entity Framework Core.

Art Ba-Bomb emphasizes practical, real-world usability with a clean interface optimized for fast-paced production environments.

Overview
Art departments move fast. Spreadsheets, texts, and scattered shopping lists quickly become difficult to manage and track during an active
production.

Art Ba-Bomb centralizes project organization into a single workflow-driven system that allows teams to:
  Create and manage projects
  Organize items by scene
  Track shopping status
  Upload receipts 
  Upload reference images
  Manage returns
  Monitor budgets
  Collaborate through role-based access

The app was intentionally designed to feel lightweight and intuitive without adding extra clutter.

Core Features
Project Management:
Create projects for feature films, television, commercials, music videos, live events, short films

Projects can be organized by department:
Set Dec & Props
(Current version only supports 2 most common departments but is built to scale to accomodate more art departments)

Scene based organization
Instead of traditional categories, items are organized by Scene to better reflect real production workflows and actually assist in prepping
for shoot days by helping organize a truck as such.

Item Workflow Tracking
Each item follows a production ready lifecycle:
Needed > Acquired > Return Queue > Returned

Track:
  Item Name
  Item Number
  Scene
  Notes & Sourcing details
  Estimated Cost (Pre-shopping)
  Actual Cost (acquired budget info)
  Item image / reference photo (Admins ability to upload image pre-shopping to ensure shoppers get correct item and acquired item images
    to share with client)
  Purchase Receipts
  Return Receipts
  Return Deadlines
This creates a clear purchasing pipeline from planning through wrap. (Future versions will include a wrapped status and page to help accounting.)

Receipt & File Management
  Upload and preview:
  Purchase receipts
  Return receipts
  Item reference images

Supported file types include:
  JPG
  JPEG
  PNG
  WEBP
  PDF

Built-in validation protects against unsupported uploads and oversized files.

Budget Visibility
Projects include financial tracking tools for:
  Estimated budget
  Actual spending
  Remaining budget

This gives department leadership immediate visibility into project costs while still allowing shoppers to stay focused on execution.

Role-Based Permissions
Art Ba-Bomb uses a structured permission system to reflect real department hierarchy.

Admin
  Full system access:
  Create/edit projects
  Manage items
  Upload files
  Manage returns
  Assign user roles
  Shopper

Workflow access:
  Create items
  Edit items
  Update shopping status
  Upload receipts
  Manage return workflow
  Production

Read-only visibility:
  View project progress
  Review purchases
  Monitor budget status

This structure protects production data while enabling collaborative workflows.

User Experience Focus
Art Ba-Bomb was built with speed and clarity as primary goals.

Key UX improvements include:
  Mobile-friendly layouts
  Responsive tables and cards
  Sticky success notifications
  Preview modals for receipts/images
  Streamlined shopper workflows
  Scene grouping for reduced visual clutter
  Smooth redirects that preserve workflow position

The interface prioritizes fast updates in high-pressure production environments.

Technology Stack

Backend
  C#
  ASP.NET Core MVC
  Entity Framework Core

Database
  MySQL

Authentication & Security
  ASP.NET Identity
  Role-based authorization

Frontend
  Razor Views
  Bootstrap
  Responsive CSS

File Handling
  Secure upload validation
  Receipt/image previews
  Persistent local file storage

Why Art Ba-Bomb?
Art departments often rely on fragmented workflows involving spreadsheets, group texts, handwritten lists, and disconnected budgeting 
systems.
Art Ba-Bomb was created to provide a purpose-built solution designed specifically for the realities of:

Tight deadlines
Constant item updates
Multiple collaborators
High-volume purchasing
Return tracking
Budget accountability

The goal is simple:

Give production designers, art directors, prop masters, and shoppers a faster, cleaner, and more collaborative way to manage production 
shopping workflows.
