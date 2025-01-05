function clearCart() {
    if (confirm('Ви впевнені, що хочете видалити весь кошик?')) {
        $.post('/Home/ClearCart')
            .done(function () {
                location.reload();
            });
    }
}

function removeFromCart(productId) {
    if (confirm('Are you sure you want to remove this item?')) {
        $.post('/Home/RemoveFromCart', { productId: productId })
            .done(function () {
                location.reload();
            });
    }
}

function removeOneFromCart(productId) {
    $.post('/Home/RemoveOneFromCart', { productId: productId })
        .done(function () {
            location.reload();
        });
}