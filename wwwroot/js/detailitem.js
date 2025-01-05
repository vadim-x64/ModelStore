var incrementInterval, decrementInterval;

function incrementQuantity() {
    var quantityInput = document.getElementById('quantity');
    quantityInput.value = parseInt(quantityInput.value) + 1;
}

function decrementQuantity() {
    var quantityInput = document.getElementById('quantity');
    if (quantityInput.value > 1) {
        quantityInput.value = parseInt(quantityInput.value) - 1;
    }
}

function startIncrement() {
    incrementQuantity();
    incrementInterval = setInterval(incrementQuantity, 100);
}

function stopIncrement() {
    clearInterval(incrementInterval);
}

function startDecrement() {
    decrementQuantity();
    decrementInterval = setInterval(decrementQuantity, 100);
}

function stopDecrement() {
    clearInterval(decrementInterval);
}

function validateQuantity() {
    var quantityInput = document.getElementById('quantity');
    var value = quantityInput.value;

    if (isNaN(value) || value < 1) {
        quantityInput.value = 1;
    }

    if (value.startsWith("0")) {
        quantityInput.value = 1;
    }
}

