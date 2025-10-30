// wwwroot/js/booking.js
(function ($) {
    document.addEventListener('DOMContentLoaded', function () {
        const $form = $('#reserveForm');
        if ($form.length === 0) return;

        const $pickup = $form.find('#pickupLocationSelect');
        const $dropoff = $form.find('#dropoffLocationSelect');
        const $category = $form.find('#categorySelect');
        const $car = $form.find('#carSelect');
        const $startDate = $form.find('input[name="StartDate"]');
        const $endDate = $form.find('input[name="EndDate"]');
        const $startTime = $form.find('select[name="StartTime"]');
        const $endTime = $form.find('select[name="EndTime"]');
        const $bookBtn = $form.find('button[type="submit"]');

        const $tenantHiddenAjax = $form.find('#tenantIdHidden');
        const $tenantHiddenPost = $form.find('#tenantId');

        let lastChosenCarId = $car.val() || null;

        function hhmm(v) { return (v && v.length === 5) ? v : "12:00"; }
        function debounce(fn, wait) { let t; return function () { clearTimeout(t); t = setTimeout(() => fn.apply(this, arguments), wait); }; }
        function setLoading(on) {
            if (on) {
                $car.prop('disabled', true).html('<option selected>Loading...</option>');
                $bookBtn.prop('disabled', true);
                $form.find('#carLostMsg, #noCarAvailableMsg').remove();
            } else {
                $car.prop('disabled', false);
                $bookBtn.prop('disabled', false);
            }
        }

        function getSelectedTenantIdFromStation() {
            const $opt = $pickup.find('option:selected');
            const raw = $opt.data('tenantid');
            const tid = Number.isFinite(raw) ? raw : parseInt(raw, 10);
            return Number.isFinite(tid) ? tid : null;
        }

        function syncTenantHidden() {
            const tid = getSelectedTenantIdFromStation();
            const val = (tid == null ? '' : String(tid));
            $tenantHiddenAjax.val(val);
            $tenantHiddenPost.val(val);
            return tid;
        }

        $car.on('change', function () {
            lastChosenCarId = $(this).val() || null;
            $form.find('#carLostMsg, #noCarAvailableMsg').remove();
        });

        function reloadCars() {
            const pickupLocationId = $pickup.val();
            const catVal = $category.val();
            const categoryId = /^\d+$/.test(catVal) ? parseInt(catVal, 10) : null;

            const startDateVal = $startDate.val();
            const endDateVal = $endDate.val();
            const startTimeVal = hhmm($startTime.val());
            const endTimeVal = hhmm($endTime.val());
            if (!pickupLocationId || !startDateVal || !endDateVal) return;

            const tid = syncTenantHidden();

            const params = {
                pickupLocationId,
                startDate: startDateVal,
                endDate: endDateVal,
                startTime: startTimeVal,
                endTime: endTimeVal
            };
            if (tid != null) params.tenantId = tid;
            if (categoryId !== null) params.categoryId = categoryId;

            const hadSelection = !!lastChosenCarId;
            setLoading(true);

            $.get('/Bookcar/AvailableCars', params)
                .done(function (data) {
                    let foundOld = false;
                    $form.find('#carLostMsg, #noCarAvailableMsg').remove();
                    $car.empty().append('<option value="">Select Your Car</option>');

                    if (!data || data.length === 0) {
                        $car.append('<option value="" disabled style="color:#dc3545;font-style:italic;">Không có xe phù hợp</option>');
                        const stationName = $pickup.find('option:selected').text() || 'station này';
                        const noCarMsg = `
                          <div id="noCarAvailableMsg" class="alert alert-warning mt-2 mb-0" role="alert">
                            <strong>Xe tạm thời không có tại ${stationName}</strong><br>
                            <small>Hãy thử station khác, thay đổi thời gian hoặc loại xe.</small>
                          </div>`;
                        $car.after(noCarMsg);
                        $bookBtn.prop('disabled', true);
                        lastChosenCarId = null;
                        return;
                    }

                    data.forEach(function (item) {
                        if (hadSelection && String(item.carId) === String(lastChosenCarId)) foundOld = true;
                        $car.append($('<option/>', { value: item.carId, text: item.carName }));
                    });

                    if (foundOld) $car.val(lastChosenCarId); else lastChosenCarId = null;
                    $bookBtn.prop('disabled', false);
                })
                .fail(function (xhr) {
                    console.error('Load cars failed:', xhr?.status, xhr?.responseText);
                    $form.find('#carLostMsg, #noCarAvailableMsg').remove();
                    $car.empty().append('<option value="" disabled style="color:#dc3545;">Lỗi tải dữ liệu</option>');
                    const errorMsg = `
                        <div id="noCarAvailableMsg" class="alert alert-danger mt-2 mb-0" role="alert">
                          <strong>Không thể tải danh sách xe</strong><br>
                          <small>Kiểm tra kết nối hoặc thử lại sau.</small>
                        </div>`;
                    $car.after(errorMsg);
                    $bookBtn.prop('disabled', true);
                })
                .always(function () { setLoading(false); });
        }

        const reloadCarsDebounced = debounce(reloadCars, 300);

        $pickup.on('change', function () {
            $dropoff.val($(this).val());
            reloadCarsDebounced();
        });
        $category.on('change', reloadCarsDebounced);
        $startDate.on('change', reloadCarsDebounced);
        $endDate.on('change', reloadCarsDebounced);
        $startTime.on('change', reloadCarsDebounced);
        $endTime.on('change', reloadCarsDebounced);

        // Init
        (function init() {
            const tid = getSelectedTenantIdFromStation();
            $tenantHiddenAjax.val(tid ?? '');
            $tenantHiddenPost.val(tid ?? '');
            reloadCars();
        })();
    });
})(jQuery);
