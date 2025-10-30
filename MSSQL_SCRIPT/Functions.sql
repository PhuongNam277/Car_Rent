-- Scalar function
-- hàm tính tuổi của một người dựa trên ngày sinh được truyền vào
create function dbo.CalculateAge (@DateOfBirth DATE)
returns int -- bắt buộc khai báo kiểu dữ liệu trả về
as
begin
	declare @Age int;
	
	select @age = DATEDIFF(year, @DateOfBirth, GETDATE()) -
				  case
						when (month(@DateOfBirth) > MONTH(GETDATE())) or
							 (month(@DateOfBirth) = MONTH(GETDATE()) and
							  day(@DateOfBirth) > day(GETDATE()))
						then 1
						else 0
					end;
	return @Age;
end
go

select dbo.CalculateAge('2004-07-27') as CalculatedAge;
go

-- table-valued function
create function dbo.GetHighValueOrders (@MinAmount MONEY)
returns table
as
return
(
	-- select các hàng được trả về
	select
		ReservationId,
		UserId,
		TotalPrice,
		ReservationDate
	from Reservations
	where 
		TotalPrice >= @MinAmount
);
go

select * from dbo.GetHighValueOrders(300) as HighOrders

