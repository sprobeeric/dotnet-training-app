// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener("DOMContentLoaded", () => {
    const form = document.querySelector("[data-purchase-form]");
    if (!form) {
        return;
    }

    const productCards = Array.from(form.querySelectorAll("[data-product-card]"));
    const quantityInputs = Array.from(form.querySelectorAll("[data-quantity-input]"));
    const searchInput = form.querySelector("[data-product-search]");
    const receivedInput = form.querySelector("[data-received-input]");
    const selectedItemTargets = Array.from(form.querySelectorAll("[data-selected-items], [data-selected-items-summary]"));
    const subtotalTarget = form.querySelector("[data-subtotal]");
    const changeTarget = form.querySelector("[data-change-preview]");
    const summaryLinesTarget = form.querySelector("[data-summary-lines]");
    const summaryEmptyTarget = form.querySelector("[data-summary-empty]");

    const updateSummary = () => {
        let selectedItems = 0;
        let subtotal = 0;
        const selectedProducts = [];

        quantityInputs.forEach((input) => {
            const quantity = Number(input.value) || 0;
            const unitPrice = Number(input.dataset.unitPrice) || 0;
            const card = input.closest("[data-product-card]");

            if (card) {
                card.classList.toggle("is-selected", quantity > 0);
            }

            if (quantity > 0) {
                selectedItems += quantity;
                subtotal += quantity * unitPrice;
                selectedProducts.push({
                    name: card?.dataset.productNameDisplay || card?.querySelector(".product-card__name")?.textContent || "",
                    quantity,
                    unitPrice,
                    lineTotal: quantity * unitPrice
                });
            }
        });

        selectedItemTargets.forEach((target) => {
            target.textContent = String(selectedItems);
        });

        if (subtotalTarget) {
            subtotalTarget.textContent = subtotal.toFixed(2);
        }

        if (changeTarget && receivedInput) {
            const received = Number(receivedInput.value) || 0;
            changeTarget.textContent = (received - subtotal).toFixed(2);
        }

        if (summaryLinesTarget && summaryEmptyTarget) {
            if (selectedProducts.length === 0) {
                summaryLinesTarget.replaceChildren();
                summaryEmptyTarget.hidden = false;
            } else {
                summaryEmptyTarget.hidden = true;
                summaryLinesTarget.replaceChildren(
                    ...selectedProducts.map((product) => {
                        const line = document.createElement("div");
                        line.className = "purchase-summary-line";

                        const details = document.createElement("div");

                        const name = document.createElement("p");
                        name.className = "purchase-summary-line__name";
                        name.textContent = product.name;

                        const meta = document.createElement("p");
                        meta.className = "purchase-summary-line__meta";
                        meta.textContent = `${product.quantity} x ${product.unitPrice.toFixed(2)}`;

                        const total = document.createElement("div");
                        total.className = "purchase-summary-line__total";
                        total.textContent = product.lineTotal.toFixed(2);

                        details.append(name, meta);
                        line.append(details, total);
                        return line;
                    })
                );
            }
        }
    };

    const filterProducts = () => {
        const query = (searchInput?.value || "").trim().toLowerCase();

        productCards.forEach((card) => {
            const productName = card.dataset.productName || "";
            card.classList.toggle("is-hidden", query.length > 0 && !productName.includes(query));
        });
    };

    quantityInputs.forEach((input) => {
        input.addEventListener("input", updateSummary);
    });

    receivedInput?.addEventListener("input", updateSummary);
    searchInput?.addEventListener("input", filterProducts);

    filterProducts();
    updateSummary();
});
