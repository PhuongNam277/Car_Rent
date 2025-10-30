-- Don gian nhat
create proc dbo.GetAllCars
as
begin
	select
		cars.CarId,
		cars.CarName,
		cars.BaseLocationId,
		cars.RentalPricePerDay,
		cars.VehicleType
		from CarRentalDB.dbo.Cars cars
end
go

exec dbo.GetAllCars
go

-- Proc co tham so dau vao
create proc dbo.GetCarById
	@CarId INT
as
begin
	select 
		cars.CarId,
		cars.CarName,
		cars.RentalPricePerDay,
		cars.Status
	from Cars cars
	where CarId = @CarId
end
go

exec dbo.GetCarById 3
go


-- proc co tham so dau vao va dau ra
create proc dbo.CountReservationsOfUser
	@UserId INT,
	@ReservationCount INT OUTPUT
as
begin
	select 
		reservations.ReservationId,
		reservations.TotalPrice
	from Reservations reservations
	where reservations.UserId = @UserId
	set @ReservationCount = @@ROWCOUNT
end
go

declare @TotalCount INT
exec dbo.CountReservationsOfUser
	@UserId = 11,
	@ReservationCount = @TotalCount OUTPUT;
select @TotalCount as TotalFound
go


-- Proc tính tổng tiền đã trả của một user
create proc dbo.CalculateUserTotalSpent
	@UserId INT,

	-- Kieu du lieu: MONEY/DECIMAL/FLOAT
	@TotalSpent MONEY OUTPUT
as
begin
	select
		@TotalSpent = SUM(reservations.TotalPrice)
	from
		Reservations reservations
	where reservations.UserId = @UserId

	if @TotalSpent IS NULL
	begin
		set @TotalSpent = 0
	end
end
go

declare @UserTotal MONEY;
exec dbo.CalculateUserTotalSpent
	@UserId = 11,
	@TotalSpent = @UserTotal OUTPUT;
select @UserTotal as UserTotalSpent
go
