// Adds small usability enhancements while the server remains the source of truth.
document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("[data-file-input]").forEach((input) => {
        const statusId = input.dataset.fileStatus;
        const status = statusId ? document.getElementById(statusId) : null;
        if (!status) return;

        input.addEventListener("change", () => {
            const file = input.files?.[0];
            if (!file) {
                status.textContent = "No file selected.";
                return;
            }

            const size = file.size < 1024 * 1024
                ? `${Math.max(1, Math.round(file.size / 1024))} KB`
                : `${(file.size / (1024 * 1024)).toFixed(1)} MB`;
            status.textContent = `${file.name} selected (${size}).`;
        });
    });

    document.querySelectorAll("[data-loading-form]").forEach((form) => {
        form.addEventListener("submit", () => {
            if (!form.checkValidity()) return;
            const button = form.querySelector("button[type='submit']");
            if (!button) return;
            button.disabled = true;
            button.classList.add("is-loading");
            button.setAttribute("aria-busy", "true");
            button.textContent = form.dataset.loadingText || "Please wait...";
        });
    });

    const calculator = document.querySelector("[data-booking-calculator]");
    const checkIn = document.querySelector("[data-check-in]");
    const checkOut = document.querySelector("[data-check-out]");
    if (calculator && checkIn && checkOut) {
        const nightCount = calculator.querySelector("[data-night-count]");
        const estimatedTotal = calculator.querySelector("[data-estimated-total]");
        const nightlyRate = Number(calculator.dataset.nightlyRate);
        const cleaningFee = Number(calculator.dataset.cleaningFee);
        const serviceFee = Number(calculator.dataset.serviceFee);
        const money = new Intl.NumberFormat("en-ZA", { style: "currency", currency: "ZAR" });

        const toUtcDay = (value) => {
            const parts = value.split("-").map(Number);
            return parts.length === 3 ? Date.UTC(parts[0], parts[1] - 1, parts[2]) : Number.NaN;
        };

        const updateEstimate = () => {
            if (checkIn.value) {
                const nextDay = new Date(toUtcDay(checkIn.value) + 86400000).toISOString().slice(0, 10);
                checkOut.min = nextDay;
            }

            const nights = Math.round((toUtcDay(checkOut.value) - toUtcDay(checkIn.value)) / 86400000);
            if (!Number.isFinite(nights) || nights < 1) {
                nightCount.textContent = "Choose valid dates";
                estimatedTotal.textContent = "Choose valid dates";
                return;
            }

            nightCount.textContent = `${nights} ${nights === 1 ? "night" : "nights"}`;
            estimatedTotal.textContent = money.format((nightlyRate * nights) + cleaningFee + serviceFee);
        };

        checkIn.addEventListener("change", updateEstimate);
        checkOut.addEventListener("change", updateEstimate);
        updateEstimate();
    }
});
