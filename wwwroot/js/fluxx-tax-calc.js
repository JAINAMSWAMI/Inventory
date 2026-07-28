/**
 * Shared line / voucher tax + discount auto-calc.
 * Discount type: "Percent" | "Amount"
 */
(function (global) {
    function num(v) {
        var n = parseFloat(String(v == null ? '' : v).replace(/,/g, ''));
        return isNaN(n) ? 0 : n;
    }

    function calc(opts) {
        var qty = num(opts.qty);
        if (qty <= 0) qty = 1;
        var rate = num(opts.rate);
        var gross = opts.gross != null ? num(opts.gross) : (qty * rate);
        var discType = (opts.discountType || 'Percent').toString();
        var discVal = num(opts.discountValue);
        var taxPct = num(opts.taxPercent);

        var discountAmt = 0;
        if (discType === 'Amount' || discType === 'Currency' || discType === '₹')
            discountAmt = discVal;
        else
            discountAmt = gross * discVal / 100;

        if (discountAmt < 0) discountAmt = 0;
        if (discountAmt > gross) discountAmt = gross;

        var discounted = gross - discountAmt;
        var tax = discounted * taxPct / 100;
        var total = discounted + tax;

        return {
            gross: gross,
            discountAmt: discountAmt,
            discounted: discounted,
            tax: tax,
            total: total
        };
    }

    function fmt(n, d) {
        d = d == null ? 4 : d;
        return (Math.round((n + Number.EPSILON) * Math.pow(10, d)) / Math.pow(10, d)).toFixed(d);
    }

    /** Bind a product-style line row (qty, rate, discount, tax%). */
    function bindLineRow(row, onChange) {
        if (!row) return;

        function refresh() {
            var qtyEl = row.querySelector('.fx-line-qty');
            var rateEl = row.querySelector('.fx-line-price, .fx-line-rate');
            var typeEl = row.querySelector('.fx-disc-type');
            var valEl = row.querySelector('.fx-disc-val');
            var taxSel = row.querySelector('.fx-tax-pct');
            var r = calc({
                qty: qtyEl && qtyEl.value,
                rate: rateEl && rateEl.value,
                discountType: typeEl && typeEl.value,
                discountValue: valEl && valEl.value,
                taxPercent: taxSel && taxSel.value
            });
            var discOut = row.querySelector('.fx-disc-price');
            var taxOut = row.querySelector('.fx-tax-amt');
            var totOut = row.querySelector('.fx-line-total');
            var discAmtHid = row.querySelector('.fx-disc-amt');
            if (discOut) discOut.textContent = isFinite(r.discounted) ? fmt(r.discounted) : '0.0000';
            if (taxOut) taxOut.textContent = fmt(r.tax);
            if (totOut) totOut.textContent = fmt(r.total);
            if (discAmtHid) discAmtHid.value = fmt(r.discountAmt);
            var taxAmtInp = row.querySelector('.fx-tax-amt-input');
            var totInp = row.querySelector('.fx-line-total-input');
            var discPriceInp = row.querySelector('.fx-disc-price-input');
            if (taxAmtInp) taxAmtInp.value = fmt(r.tax);
            if (totInp) totInp.value = fmt(r.total);
            if (discPriceInp) discPriceInp.value = fmt(r.discounted);
            if (typeof onChange === 'function') onChange(r);
        }

        if (!row._fxTaxBound) {
            row._fxTaxBound = true;
            row.addEventListener('input', function (e) {
                if (e.target.matches('.fx-line-qty, .fx-line-price, .fx-line-rate, .fx-disc-val, .fx-tax-pct'))
                    refresh();
            });
            row.addEventListener('change', function (e) {
                if (e.target.matches('.fx-disc-type, .fx-tax-pct'))
                    refresh();
            });
        }
        row._fxTaxRefresh = refresh;
        refresh();
    }

    /** Bind voucher-level amount block (#fxTaxBlock). */
    function bindVoucherBlock(root, onChange) {
        root = root || document.getElementById('fxTaxBlock');
        if (!root || root._fxTaxBound) return;
        root._fxTaxBound = true;

        function refresh() {
            var grossEl = root.querySelector('#TaxableAmount, .fx-gross');
            var typeEl = root.querySelector('.fx-disc-type');
            var valEl = root.querySelector('.fx-disc-val');
            var taxSel = root.querySelector('.fx-tax-pct');
            var r = calc({
                gross: grossEl && grossEl.value,
                qty: 1,
                rate: grossEl && grossEl.value,
                discountType: typeEl && typeEl.value,
                discountValue: valEl && valEl.value,
                taxPercent: taxSel && taxSel.value
            });
            var discOut = root.querySelector('.fx-disc-price');
            var taxOut = root.querySelector('.fx-tax-amt');
            var payEl = document.getElementById('PaymentAmount');
            var discAmt = root.querySelector('#DiscountAmount');
            var taxAmt = root.querySelector('#TaxAmount');
            var taxPctHid = root.querySelector('#TaxPercent');
            if (discOut) discOut.value = fmt(r.discounted);
            if (taxOut) taxOut.value = fmt(r.tax);
            if (discAmt) discAmt.value = fmt(r.discountAmt);
            if (taxAmt) taxAmt.value = fmt(r.tax);
            if (taxPctHid) taxPctHid.value = taxSel ? taxSel.value : '0';
            if (payEl) payEl.value = fmt(r.total);
            if (typeof onChange === 'function') onChange(r);
        }

        root.addEventListener('input', refresh);
        root.addEventListener('change', refresh);
        refresh();
        return { refresh: refresh };
    }

    global.fluxxTaxCalc = { calc: calc, fmt: fmt, bindLineRow: bindLineRow, bindVoucherBlock: bindVoucherBlock };
})(window);
