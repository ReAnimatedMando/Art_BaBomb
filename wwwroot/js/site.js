document.addEventListener("DOMContentLoaded", function () {
    const receiptModal = document.getElementById("receiptPreviewModal");
    const receiptImage = document.getElementById("receiptPreviewImage");
    const receiptTitle = document.getElementById("receiptPreviewModalLabel");

    if (receiptModal) {
        receiptModal.addEventListener("show.bs.modal", function (event) {
            const trigger = event.relatedTarget;
            const src = trigger.getAttribute("data-receipt-src");
            const title = trigger.getAttribute("data-receipt-title") || "Receipt Preview";

            receiptImage.src = src;
            receiptTitle.textContent = title;
        });
    }
});

document.addEventListener("DOMContentLoaded", function () {
    const itemModal = document.getElementById("itemPreviewModal");
    const itemImage = document.getElementById("itemPreviewImage");
    const itemTitle = document.getElementById("itemPreviewModalLabel");

    if (itemModal) {
        itemModal.addEventListener("show.bs.modal", function (event) {
            const trigger = event.relatedTarget;
            const src = trigger.getAttribute("data-item-src");
            const title = trigger.getAttribute("data-item-title") || "Item Preview";

            itemImage.src = src;
            itemTitle.textContent = title;
        });
    }
});