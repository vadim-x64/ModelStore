document.getElementById('showPassword').addEventListener('change', function () {
    var passwordField = document.getElementById('Password');
    if (this.checked) {
        passwordField.type = 'text';
    } else {
        passwordField.type = 'password';
    }
});

document.querySelector('.registration-form').addEventListener('submit', function (e) {
    e.preventDefault();
    var formData = new FormData(this);
    fetch('/Home/Register', {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        var errorMessage = document.getElementById('error-message');
        if (data.success) {
            window.location.href = data.redirectUrl;
        } else {
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

document.querySelectorAll('input').forEach(input => {
    input.addEventListener('contextmenu', function (e) {
        e.preventDefault();
    });
});

function allowOnlyLetters(e) {
    let input = e.target;
    input.value = input.value.replace(/[^A-Za-zА-Яа-яЁёЇїІіЄєҐґ]/g, '');
}

document.addEventListener('DOMContentLoaded', function () {
    var today = new Date();
    var dd = String(today.getDate()).padStart(2, '0');
    var mm = String(today.getMonth() + 1).padStart(2, '0');
    var yyyy = today.getFullYear();
    today = yyyy + '-' + mm + '-' + dd;
    document.getElementById('BirthDate').setAttribute('max', today);
});

document.getElementById('Phone').addEventListener('input', function(e) {
    let input = e.target;
    let value = input.value;
    if (!value) {
        input.value = '';
        return;
    }
    value = value.replace(/\+/g, '');
    value = '+' + value;
    value = '+' + value.substring(1).replace(/\D/g, '');
    if (value.length > 1 && !value.startsWith('+380')) {
        value = '+380' + value.substring(1).replace(/[^0-9]/g, '');
    }
    if (value.length > 13) {
        value = value.slice(0, 13);
    }
    input.value = value;
});

document.getElementById('Password').addEventListener('input', function () {
    var password = this.value;
    var strengthBar = document.getElementById('password-strength-bar');
    var strengthText = document.getElementById('password-strength-text');
    var regexWeak = /^(?=.*[a-zA-Zа-яА-ЯЇїІіЄєҐґ]).{8,}$/;
    var regexModerate = /^(?=.*[a-zA-Zа-яА-ЯЇїІіЄєҐґ])(?=.*\d).{8,}$/;
    var regexStrong = /^(?=.*[a-zA-Zа-яА-ЯЇїІіЄєҐґ])(?=.*\d)(?=.*[!#$%^&*()_+={}\[\]:;'"<>,.?/\\|`~]).{8,}$/;
    if (password.length === 0) {
        strengthBar.style.width = '0%';
        strengthBar.style.backgroundColor = 'transparent';
    } else if (regexStrong.test(password)) {
        strengthBar.style.width = '100%';
        strengthBar.style.backgroundColor = 'green';
    } else if (regexModerate.test(password)) {
        strengthBar.style.width = '70%';
        strengthBar.style.backgroundColor = 'orange';
    } else if (regexWeak.test(password)) {
        strengthBar.style.width = '35%';
        strengthBar.style.backgroundColor = 'red';
    } else {
        strengthBar.style.width = '0%';
        strengthBar.style.backgroundColor = 'transparent';
    }
});

document.getElementById('generatePasswordBtn').addEventListener('click', function () {
    var password = generateStrongPassword();
    document.getElementById('Password').value = password;
    updatePasswordStrength(password);
});

function generateStrongPassword() {
    var length = 16;
    var charsetLowercase = "abcdefghijklmnopqrstuvwxyz";
    var charsetUppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    var charsetDigits = "0123456789";
    var charsetSpecial = "!#$%^&*()_+[]{}|;:,.<>?/";
    var charsetUkrainian = "аабвгдеєєжзиіїклмнопрстуфхцчшщьюяАБВГДЕЄЖЗИЇКЛМНОПРСТУФХЦЧШЩЬЮЯ";
    var charset = charsetLowercase + charsetUppercase + charsetDigits + charsetSpecial + charsetUkrainian;
    var password = "";
    password += getRandomChar(charsetLowercase);
    password += getRandomChar(charsetUppercase);
    password += getRandomChar(charsetDigits);
    password += getRandomChar(charsetSpecial);
    password += getRandomChar(charsetUkrainian);
    for (var i = password.length; i < length; i++) {
        password += getRandomChar(charset);
    }
    return shuffleString(password);
}

function getRandomChar(charset) {
    var randomIndex = Math.floor(Math.random() * charset.length);
    return charset[randomIndex];
}

function shuffleString(str) {
    var arr = str.split('');
    for (var i = arr.length - 1; i > 0; i--) {
        var j = Math.floor(Math.random() * (i + 1));
        [arr[i], arr[j]] = [arr[j], arr[i]];
    }
    return arr.join('');
}

function updatePasswordStrength(password) {
    var strengthBar = document.getElementById('password-strength-bar');
    var regexWeak = /^(?=.*[a-zA-Zа-яА-ЯЇїІіЄєҐґ]).{8,}$/;
    var regexModerate = /^(?=.*[a-zA-Zа-яА-ЯЇїІіЄєҐґ])(?=.*\d).{8,}$/;
    var regexStrong = /^(?=.*[a-zA-Zа-яА-ЯЇїІіЄєҐґ])(?=.*\d)(?=.*[!#$%^&*()_+={}\[\]:;'"<>,.?/\\|`~]).{8,}$/;
    if (password.length === 0) {
        strengthBar.style.width = '0%';
        strengthBar.style.backgroundColor = 'transparent';
    } else if (regexStrong.test(password)) {
        strengthBar.style.width = '100%';
        strengthBar.style.backgroundColor = 'green';
    } else if (regexModerate.test(password)) {
        strengthBar.style.width = '70%';
        strengthBar.style.backgroundColor = 'orange';
    } else if (regexWeak.test(password)) {
        strengthBar.style.width = '35%';
        strengthBar.style.backgroundColor = 'red';
    } else {
        strengthBar.style.width = '0%';
        strengthBar.style.backgroundColor = 'transparent';

    }
}