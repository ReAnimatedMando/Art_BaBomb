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

document.addEventListener("DOMContentLoaded", function () {
  const projectDetailsPage = document.getElementById("project-details-page");

  if (!projectDetailsPage) {
    return;
  }

  const adjustQtyUrl = projectDetailsPage.dataset.adjustQtyUrl;
  const updateDescriptionUrl = projectDetailsPage.dataset.updateDescriptionUrl;

  const receiptModal = document.getElementById("receiptPreviewModal");
  const receiptImage = document.getElementById("receiptPreviewImage");
  const receiptTitle = document.getElementById("receiptPreviewModalLabel");

  if (receiptModal && receiptImage && receiptTitle) {
    receiptModal.addEventListener("show.bs.modal", function (event) {
      const trigger = event.relatedTarget;
      if (!trigger) return;

      const src = trigger.getAttribute("data-receipt-src");
      const title = trigger.getAttribute("data-receipt-title") || "Receipt Preview";

      receiptImage.src = src || "";
      receiptTitle.textContent = title;
    });
  }

  let activeItemId = null;

  async function triggerQtyUpdate(itemId, delta) {
    const tokenInput = document.querySelector('#antiForgeryForm input[name="__RequestVerificationToken"]');
    const qtyValue = document.getElementById(`qty-value-${itemId}`);

    if (!tokenInput || !qtyValue || !adjustQtyUrl) {
      return;
    }

    try {
      const response = await fetch(adjustQtyUrl, {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
          "RequestVerificationToken": tokenInput.value,
          "X-Requested-With": "XMLHttpRequest"
        },
        body: new URLSearchParams({
          id: itemId,
          delta: delta
        })
      });

      if (!response.ok) {
        throw new Error("Quantity update failed.");
      }

      const result = await response.json();

      if (result.success) {
        qtyValue.textContent = result.quantity;

        const stepper = document.querySelector(`.qty-stepper[data-item-id="${itemId}"]`);
        const minusBtn = stepper?.querySelector('.qty-adjust-btn[data-delta="-1"]');

        qtyValue.classList.add("text-success");
        setTimeout(() => qtyValue.classList.remove("text-success"), 200);

        if (minusBtn) {
          minusBtn.disabled = result.quantity <= 1;
        }
      }
    } catch (error) {
      console.error(error);
      alert("Could not update quantity. Please try again.");
    }
  }

  document.querySelectorAll(".qty-adjust-btn").forEach(button => {
    button.addEventListener("click", function () {
      const itemId = this.dataset.id;
      const delta = this.dataset.delta;

      triggerQtyUpdate(itemId, delta);
    });
  });

  document.querySelectorAll(".item-row").forEach(row => {
    row.addEventListener("mouseenter", function () {
      activeItemId = row.dataset.itemId;
      row.classList.add("table-active");
    });

    row.addEventListener("mouseleave", function () {
      activeItemId = null;
      row.classList.remove("table-active");
    });

    row.addEventListener("focus", function () {
      activeItemId = row.dataset.itemId;
      row.classList.add("table-active");
    });

    row.addEventListener("blur", function () {
      activeItemId = null;
      row.classList.remove("table-active");
    });
  });

  document.addEventListener("keydown", function (e) {
    if (!activeItemId) return;

    if (e.key === "+" || e.key === "=") {
      triggerQtyUpdate(activeItemId, 1);
      e.preventDefault();
    }

    if (e.key === "-" || e.key === "_") {
      triggerQtyUpdate(activeItemId, -1);
      e.preventDefault();
    }
  });

  document.querySelectorAll(".edit-description-btn").forEach(button => {
    button.addEventListener("click", function () {
      const itemId = this.dataset.id;
      const display = document.getElementById(`description-display-${itemId}`);
      const editor = document.getElementById(`description-editor-${itemId}`);
      const editBtn = this;

      if (display) display.classList.add("d-none");
      if (editor) editor.classList.remove("d-none");
      editBtn.classList.add("d-none");
    });
  });

  document.querySelectorAll(".cancel-description-btn").forEach(button => {
    button.addEventListener("click", function () {
      const itemId = this.dataset.id;
      const display = document.getElementById(`description-display-${itemId}`);
      const editor = document.getElementById(`description-editor-${itemId}`);
      const editBtn = document.querySelector(`.edit-description-btn[data-id="${itemId}"]`);
      const input = document.getElementById(`description-input-${itemId}`);

      if (editor) editor.classList.add("d-none");
      if (display) display.classList.remove("d-none");
      if (editBtn) editBtn.classList.remove("d-none");

      if (input && display) {
        input.value = display.textContent.trim() === "No notes"
          ? ""
          : display.textContent.trim();
      }
    });
  });

  document.querySelectorAll(".save-description-btn").forEach(button => {
    button.addEventListener("click", async function () {
      const itemId = this.dataset.id;
      const input = document.getElementById(`description-input-${itemId}`);
      const display = document.getElementById(`description-display-${itemId}`);
      const editor = document.getElementById(`description-editor-${itemId}`);
      const editBtn = document.querySelector(`.edit-description-btn[data-id="${itemId}"]`);
      const tokenInput = document.querySelector('#antiForgeryForm input[name="__RequestVerificationToken"]');

      if (!input || !display || !tokenInput || !updateDescriptionUrl) {
        return;
      }

      try {
        const response = await fetch(updateDescriptionUrl, {
          method: "POST",
          headers: {
            "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
            "RequestVerificationToken": tokenInput.value,
            "X-Requested-With": "XMLHttpRequest"
          },
          body: new URLSearchParams({
            id: itemId,
            description: input.value
          })
        });

        if (!response.ok) {
          throw new Error("Description update failed.");
        }

        const result = await response.json();

        if (result.success) {
          display.innerHTML = result.description && result.description.trim() !== ""
            ? result.description
            : '<span class="fst-italic">No notes</span>';

          display.classList.remove("d-none");
          if (editor) editor.classList.add("d-none");
          if (editBtn) editBtn.classList.remove("d-none");

          display.classList.add("text-success");
          setTimeout(() => display.classList.remove("text-success"), 250);
        }
      } catch (error) {
        console.error(error);
        alert("Could not update notes. Please try again.");
      }
    });
  });

  document.querySelectorAll(".mobile-edit-description-btn").forEach(button => {
  button.addEventListener("click", function () {
    const itemId = this.dataset.id;
    const display = document.getElementById(`mobile-description-display-${itemId}`);
    const editor = document.getElementById(`mobile-description-editor-${itemId}`);
    const editBtn = this;
    const toggleBtn = document.querySelector(`.note-toggle-btn[data-target="mobile-description-display-${itemId}"]`);

    if (display) display.classList.add("d-none");
    if (editor) editor.classList.remove("d-none");
    editBtn.classList.add("d-none");
    if (toggleBtn) toggleBtn.classList.add("d-none");

  });
});

document.querySelectorAll(".mobile-cancel-description-btn").forEach(button => {
  button.addEventListener("click", function () {
    const itemId = this.dataset.id;
    const display = document.getElementById(`mobile-description-display-${itemId}`);
    const editor = document.getElementById(`mobile-description-editor-${itemId}`);
    const editBtn = document.querySelector(`.mobile-edit-description-btn[data-id="${itemId}"]`);
    const input = document.getElementById(`mobile-description-input-${itemId}`);
    const toggleBtn = document.querySelector(`.note-toggle-btn[data-target="mobile-description-display-${itemId}"]`);

    if (editor) editor.classList.add("d-none");
    if (display) display.classList.remove("d-none");
    if (editBtn) editBtn.classList.remove("d-none");
    if (toggleBtn) toggleBtn.classList.remove("d-none");

    if (input && display) {
      input.value = display.textContent.trim() === "No notes"
        ? ""
        : display.textContent.trim();
    }
  });
});

document.querySelectorAll(".mobile-save-description-btn").forEach(button => {
  button.addEventListener("click", async function () {
    const itemId = this.dataset.id;
    const input = document.getElementById(`mobile-description-input-${itemId}`);
    const display = document.getElementById(`mobile-description-display-${itemId}`);
    const editor = document.getElementById(`mobile-description-editor-${itemId}`);
    const editBtn = document.querySelector(`.mobile-edit-description-btn[data-id="${itemId}"]`);
    const tokenInput = document.querySelector('#antiForgeryForm input[name="__RequestVerificationToken"]');
    const toggleBtn = document.querySelector(`.note-toggle-btn[data-target="mobile-description-display-${itemId}"]`);

    if (!input || !display || !tokenInput || !updateDescriptionUrl) {
      return;
    }

    try {
      const response = await fetch(updateDescriptionUrl, {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
          "RequestVerificationToken": tokenInput.value,
          "X-Requested-With": "XMLHttpRequest"
        },
        body: new URLSearchParams({
          id: itemId,
          description: input.value
        })
      });

      if (!response.ok) {
        throw new Error("Description update failed.");
      }

      const result = await response.json();

      if (result.success) {
        display.innerHTML = result.description && result.description.trim() !== ""
          ? result.description
          : '<span class="fst-italic">No notes</span>';

        display.classList.remove("d-none");
        if (editor) editor.classList.add("d-none");
        if (editBtn) editBtn.classList.remove("d-none");
        if (toggleBtn) toggleBtn.classList.remove("d-none");

        display.classList.add("text-success");
        setTimeout(() => display.classList.remove("text-success"), 250);
      }
    } catch (error) {
      console.error(error);
      alert("Could not update notes. Please try again.");
    }
  });
});

document.querySelectorAll(".note-toggle-btn").forEach(button => {
  button.addEventListener("click", function () {
    const targetId = this.dataset.target;
    const target = document.getElementById(targetId);

    if (!target) {
      return;
    }

    const isExpanded = this.dataset.expanded === "true";

    if (isExpanded) {
      target.classList.add("is-collapsed");
      this.dataset.expanded = "false";
      this.textContent = "Show More";
    } else {
      target.classList.remove("is-collapsed");
      this.dataset.expanded = "true";
      this.textContent = "Show Less";
    }
  });
});

document.querySelectorAll(".mobile-inline-description-input").forEach(input => {
  input.addEventListener("keydown", function (e) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();

      const itemId = this.id.replace("mobile-description-input-", "");
      const saveButton = document.querySelector(`.mobile-save-description-btn[data-id="${itemId}"]`);

      if (saveButton) {
        saveButton.click();
      }
    }
  });
});

  document.querySelectorAll(".inline-description-input").forEach(input => {
    input.addEventListener("keydown", function (e) {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();

        const itemId = this.id.replace("description-input-", "");
        const saveButton = document.querySelector(`.save-description-btn[data-id="${itemId}"]`);

        if (saveButton) {
          saveButton.click();
        }
      }
    });
  });
});