(() => {
    const formatDate = (value) => {
        if (!value) {
            return "";
        }

        const parts = value.split("-");
        return parts.length === 3 ? `${parts[0]}/${parts[1]}/${parts[2]}` : value;
    };

    document.querySelectorAll("[data-date-source]").forEach((source) => {
        const key = source.dataset.dateSource;
        const display = document.querySelector(`[data-date-display="${key}"]`);

        if (!display) {
            return;
        }

        const syncDisplay = () => {
            display.value = formatDate(source.value);
        };

        const openPicker = () => {
            if (typeof source.showPicker === "function") {
                source.showPicker();
                return;
            }

            source.focus();
            source.click();
        };

        syncDisplay();
        source.addEventListener("change", syncDisplay);
        display.addEventListener("click", openPicker);
    });
})();