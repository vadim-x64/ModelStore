document.querySelector('.login-form').addEventListener('submit', function (e) {
    e.preventDefault();
    var formData = new FormData(this);
    fetch('/Home/Login', {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            window.location.href = data.redirectUrl;
        } else {
            var errorMessage = document.getElementById('error-message');
            errorMessage.innerText = data.message;
            errorMessage.classList.add('show');
            setTimeout(function () {
                errorMessage.classList.remove('show');
            }, 3000);
        }
    })
    .catch(error => {
        var errorMessage = document.getElementById('error-message');
        errorMessage.classList.add('show');
        setTimeout(function () {
            errorMessage.classList.remove('show');
        }, 5000);
    });
});

document.getElementById('showPassword').addEventListener('change', function () {
    var passwordField = document.getElementById('Password');
    if (this.checked) {
        passwordField.type = 'text';
    } else {
        passwordField.type = 'password';
    }
});

document.querySelectorAll('input').forEach(input => {
    input.addEventListener('contextmenu', function (e) {
        e.preventDefault();
    });
});