-- Bảng Customers (chỉ giữ lại 2 cột đơn giản để minh họa)
-- CREATE TABLE Customers (CustomerId INT PRIMARY KEY, CustomerName NVARCHAR(100));

-- Bảng để lưu lịch sử thay đổi
CREATE TABLE CustomerAudit (
    AuditId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT,
    OldName NVARCHAR(100),
    NewName NVARCHAR(100),
    ChangeDate DATETIME DEFAULT GETDATE(),
    ChangeBy NVARCHAR(100) DEFAULT SUSER_SNAME() -- Lấy tên người dùng SQL
);
GO


-- trigger
create trigger TR_Customer_UpdateAudit
on Customers
after update
as
begin
	if exists (select * from inserted)
	begin
		-- Lấy dữ liệu tên CŨ (từ bảng deleted) và tên MỚI (từ bảng inserted)
		-- Chú ý: Trigger DML hoạt động với SETS OF ROWS, không phải từng hàng một.
	
		insert into CustomerAudit (
			CustomerId,
			OldName,
			NewName
		)
		select
			i.CustomerId,
			d.CustomerName as OldName, -- tên cũ nằm trong deleted
			i.CustomerName as NewName  -- tên mới nằm trong inserted
		from	
			inserted i -- bản ghi sau khi update
		inner join
			deleted d on i.CustomerId = d.CustomerId -- bản ghi trước khi update
		where
			-- chỉ ghi log nếu tên khách hàng thực sự thay đổi
			i.CustomerName <> d.CustomerName
	end
end
go

select * from Customers where CustomerId = 101;

update Customers
set CustomerName = N'Nguyen Phuong Nam'
where CustomerId = 101;

select * from Customers where CustomerId = 101;

SELECT * FROM CustomerAudit;
