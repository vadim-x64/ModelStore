function toggleVisibility(element, isVisible, timeout = 0) {
    if (isVisible) {
        element.classList.add('show');
        if (timeout) {
            setTimeout(() => element.classList.remove('show'), timeout);
        }
    } else {
        element.classList.remove('show');
    }
}

function setPasswordFieldType(passwordField, type) {
    passwordField.type = type;
}

function validatePassword(password) {
    const regexLowerCase = /[a-zа-яїієґ]/;
    const regexUpperCase = /[A-ZА-ЯЇІЄҐ]/;
    const regexDigits = /\d/;
    const regexSpecial = /[!#$%^&*()_+={}\[\]:;'"<>,.?/\\|`~]/;
    return {
        isLongEnough: password.length >= 8,
        hasLowerCase: regexLowerCase.test(password),
        hasUpperCase: regexUpperCase.test(password),
        hasDigits: regexDigits.test(password),
        hasSpecial: regexSpecial.test(password),
    };
}

function updatePasswordStrength(password) {
    const strengthBar = document.getElementById('password-strength-bar');
    const { isLongEnough, hasLowerCase, hasUpperCase, hasDigits, hasSpecial } = validatePassword(password);

    if (!password) {
        strengthBar.style.width = '0%';
        strengthBar.style.backgroundColor = 'transparent';
    } else if (isLongEnough && hasLowerCase && hasUpperCase && hasDigits && hasSpecial) {
        strengthBar.style.width = '100%';
        strengthBar.style.backgroundColor = 'green';
    } else if (isLongEnough && hasLowerCase && hasUpperCase && hasDigits) {
        strengthBar.style.width = '70%';
        strengthBar.style.backgroundColor = 'orange';
    } else if (isLongEnough) {
        strengthBar.style.width = '35%';
        strengthBar.style.backgroundColor = 'red';
    } else {
        strengthBar.style.width = '0%';
        strengthBar.style.backgroundColor = 'transparent';
    }
}

function formatPhoneNumber(input) {
    const phoneField = document.querySelector("input[name='Phone']");
    if (phoneField) {
        phoneField.addEventListener("input", () => {
            let value = phoneField.value.replace(/[^\d+]/g, "");

            value = value.replace(/(?!^\+)\+/g, "");

            if (value.includes("+") && value.indexOf("+") !== 0) {
                value = value.replace(/\+/g, "");
            }

            if (!value.startsWith("+380")) {
                value = value.replace(/\+380/g, "");
            }

            if (value.length > 13) {
                value = value.slice(0, 13);
            }

            if (value.length !== 13 || !value.startsWith("+380")) {
                phoneField.setCustomValidity("Неправильний формат телефону. Формат: +380ххххххххх");
            } else {
                phoneField.setCustomValidity("");
            }

            phoneField.value = value;
        });
    }
}

function allowOnlyLetters(e) {
    e.target.value = e.target.value.replace(/[^A-Za-zА-Яа-яЁёЇїІіЄєҐґ]/g, '');
}

function setMaxDateForBirthDate() {
    const today = new Date();
    const formattedDate = today.toISOString().split('T')[0];
    document.getElementById('BirthDate').setAttribute('max', formattedDate);
}

document.addEventListener('DOMContentLoaded', setMaxDateForBirthDate);

document.getElementById('showPassword').addEventListener('change', function () {
    const passwordField = document.getElementById('Password');
    setPasswordFieldType(passwordField, this.checked ? 'text' : 'password');
});

document.getElementById('Password').addEventListener('input', function () {
    updatePasswordStrength(this.value);
});

document.getElementById('generatePasswordBtn').addEventListener('click', function () {
    const password = generateStrongPassword();
    const passwordField = document.getElementById('Password');
    passwordField.value = password;
    updatePasswordStrength(password);
});

document.getElementById('Phone').addEventListener('input', function (e) {
    formatPhoneNumber(e.target);
});

document.querySelectorAll('input').forEach(input => {
    input.addEventListener('contextmenu', e => e.preventDefault());
});

document.querySelector('.registration-form').addEventListener('submit', function (e) {
    e.preventDefault();

    const password = document.getElementById('Password').value;
    const errorMessage = document.getElementById('error-message');
    const { isLongEnough, hasLowerCase, hasUpperCase, hasDigits, hasSpecial } = validatePassword(password);

    if (!(isLongEnough && hasLowerCase && hasUpperCase && hasDigits && hasSpecial)) {
        errorMessage.innerText = "Пароль має містити мінімум 8 символів, великі та малі літери, числа і спеціальні символи.";
        toggleVisibility(errorMessage, true, 5000);
        return;
    }

    const formData = new FormData(this);
    fetch('/Home/Register', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                window.location.href = data.redirectUrl;
            } else {
                errorMessage.innerText = data.message;
                toggleVisibility(errorMessage, true, 3000);
            }
        })
        .catch(() => {
            errorMessage.innerText = "Сталася помилка, спробуйте знову.";
            toggleVisibility(errorMessage, true, 5000);
    });
});

function generateStrongPassword() {
    const length = 16;
    const charsets = {
        lowercase: "abcdefghijklmnopqrstuvwxyz",
        uppercase: "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
        digits: "0123456789",
        special: "!#$%^&*()_+[]{}|;:,.<>?/",
        ukrainian: "аабвгдеєєжзиіїклмнопрстуфхцчшщьюяАБВГДЕЄЖЗИЇКЛМНОПРСТУФХЦЧШЩЬЮЯ"
    };
    const allCharsets = Object.values(charsets).join('');
    let password = '';
    Object.values(charsets).forEach(charset => {
        password += getRandomChar(charset);
    });

    for (let i = password.length; i < length; i++) {
        password += getRandomChar(allCharsets);
    }
    return shuffleString(password);
}

function getRandomChar(charset) {
    return charset[Math.floor(Math.random() * charset.length)];
}

function shuffleString(str) {
    const arr = str.split('');
    for (let i = arr.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [arr[i], arr[j]] = [arr[j], arr[i]];
    }
    return arr.join('');
}