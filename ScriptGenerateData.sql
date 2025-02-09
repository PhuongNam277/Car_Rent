-- Insert dữ liệu mẫu
INSERT INTO Users (FullName, Username, PasswordHash, Email, PhoneNumber, Role) VALUES 
('John Doe', 'johndoe', 'hashed_password', 'johndoe@example.com', '123456789', 'Customer');

INSERT INTO Categories (CategoryName) VALUES 
('Sedan');

INSERT INTO Cars (CarName, Brand, Model, LicensePlate, CategoryId, ImageUrl, RentalPricePerDay, Status) VALUES 
('Toyota Camry', 'Toyota', 'Camry', 'XYZ-123', 1, 'camry.jpg', 50.00, 'Available');

INSERT INTO Reservations (UserId, CarId, StartDate, EndDate, TotalPrice, Status) VALUES 
(1, 1, '2024-12-27', '2024-12-30', 150.00, 'Pending');

INSERT INTO Payments (ReservationId, PaymentDate, Amount, PaymentMethod, Status) VALUES 
(1, GETDATE(), 150.00, 'Credit Card', 'Paid');

INSERT INTO MaintenanceRecords (CarId, MaintenanceDate, Description, Cost) VALUES 
(1, GETDATE(), 'Oil change and tire rotation', 75.00);

INSERT INTO Reviews (CarId, UserId, Rating, Comment, ReviewDate) VALUES 
(1, 1, 5, 'Great car, very clean and comfortable.', GETDATE());

INSERT INTO Promotions (Code, Description, DiscountPercent, StartDate, EndDate, Status) VALUES 
('NEWYEAR2025', 'New Year Discount', 10.00, '2024-12-31', '2025-01-10', 'Active');

INSERT INTO Blog (Title, Content, ImageUrl, AuthorId, PublishedDate, Status) VALUES 
('Top 5 Tips for Renting a Car', 'Content about renting cars.', 'tips.jpg', 1, GETDATE(), 'Published');

INSERT INTO Contact (Name, Email, PhoneNumber, Message, SubmittedDate, Status) VALUES 
('Alice Johnson', 'alice@example.com', '987654321', 'I have a question about car rentals.', GETDATE(), 'Pending');