// Cascading State → City select
window.fluxxBindCityCascade = function (stateSelector, citySelector, citiesUrl, selectedCityId) {
    const $state = $(stateSelector);
    const $city = $(citySelector);

    function loadCities(stateId, selected) {
        $city.empty().append($('<option/>').val('').text('-- Select city --'));
        if (!stateId) return;

        $.getJSON(citiesUrl, { stateId: stateId }, function (data) {
            (data || []).forEach(function (c) {
                const opt = $('<option/>').val(c.id).text(c.name);
                if (selected && String(selected) === String(c.id)) opt.prop('selected', true);
                $city.append(opt);
            });
        });
    }

    $state.on('change', function () {
        loadCities($(this).val(), null);
    });

    if ($state.val()) {
        loadCities($state.val(), selectedCityId || $city.val());
    }
};
