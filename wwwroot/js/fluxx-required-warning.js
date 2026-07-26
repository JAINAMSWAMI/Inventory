/**
 * App-wide required-field warning (matches ERP "Please Fill Required Detail." toast).
 * Intercepts form submit; skips login/auth forms and forms marked data-skip-fluxx-validate.
 */
(function () {
    function showRequiredWarning() {
        if (typeof Swal === 'undefined') {
            alert('Please Fill Required Detail.');
            return;
        }
        Swal.fire({
            icon: 'warning',
            title: 'Warning',
            text: 'Please Fill Required Detail.',
            confirmButtonText: 'CLOSE',
            confirmButtonColor: '#f59e0b',
            iconColor: '#f59e0b'
        });
    }

    function isSkipped(form) {
        if (!form || form.tagName !== 'FORM') return true;
        if (form.getAttribute('data-skip-fluxx-validate') === 'true') return true;
        var action = (form.getAttribute('action') || '').toLowerCase();
        if (action.indexOf('/login') >= 0 || action.indexOf('/signup') >= 0) return true;
        return false;
    }

    function amountInvalid(el) {
        if (!el) return false;
        var name = (el.getAttribute('name') || el.id || '').toLowerCase();
        if (name.indexOf('paymentamount') < 0 && name.indexOf('order_total') < 0 && name !== 'amount')
            return false;
        var n = parseFloat(String(el.value || '').replace(/,/g, ''));
        return isNaN(n) || n <= 0;
    }

    function formMissingRequired(form) {
        var controls = form.querySelectorAll('input, select, textarea');
        for (var i = 0; i < controls.length; i++) {
            var el = controls[i];
            if (el.disabled || el.type === 'hidden' || el.type === 'submit' || el.type === 'button') continue;

            if (el.required || el.getAttribute('aria-required') === 'true') {
                if (el.type === 'checkbox' || el.type === 'radio') {
                    var group = form.querySelectorAll('[name="' + el.name + '"]');
                    var any = false;
                    group.forEach(function (g) { if (g.checked) any = true; });
                    if (!any) return true;
                } else if (!String(el.value || '').trim()) {
                    return true;
                }
            }

            if (amountInvalid(el)) return true;
        }

        // Autocomplete party (hidden id) on payment / expense vouchers
        if (form.querySelector('#PaymentAmount, [name="PaymentAmount"]')) {
            var partyId = form.querySelector('#PartyId, [name="PartyId"]');
            if (partyId && !String(partyId.value || '').trim()) return true;
        }

        return !form.checkValidity();
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('form').forEach(function (form) {
            if (isSkipped(form)) return;
            form.setAttribute('novalidate', 'novalidate');
        });
    });

    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (isSkipped(form)) return;

        if (formMissingRequired(form)) {
            e.preventDefault();
            e.stopPropagation();
            showRequiredWarning();

            var firstBad = form.querySelector(':invalid, [required]:not([disabled])');
            if (firstBad && typeof firstBad.focus === 'function') {
                try { firstBad.focus(); } catch (_) { /* ignore */ }
            }
            return false;
        }
    }, true);

    window.fluxxShowRequiredWarning = showRequiredWarning;
})();
