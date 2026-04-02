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