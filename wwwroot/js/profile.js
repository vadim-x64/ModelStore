document.addEventListener('DOMContentLoaded', function () {
    const toggleButtons = document.querySelectorAll('.toggle-password');
    const generatePasswordBtn = document.getElementById('generatePasswordBtn');
    const newPasswordInput = document.getElementById('newPassword');
    const errorMessage = document.getElementById('error-message');
    const confirmPasswordInput = document.getElementById('confirmPassword');

    toggleButtons.forEach(button => {
        button.addEventListener('click', function () {
            const targetId = this.getAttribute('data-target');
            const passwordInput = document.getElementById(targetId);

            if (passwordInput.type === 'password') {
                passwordInput.type = 'text';
                this.classList.remove('fa-eye');
                this.classList.add('fa-eye-slash');
            } else {
                passwordInput.type = 'password';
                this.classList.remove('fa-eye-slash');
                this.classList.add('fa-eye');
            }
        });
    });

    newPasswordInput.addEventListener('input', function () {
        const password = this.value;
        const validation = validatePassword(password);
        updatePasswordStrength(password);

        if (password.length > 0 && !validation.isLongEnough) {
            showError("Пароль має містити мінімум 8 символів");
        }
    });

    confirmPasswordInput.addEventListener('input', function () {
        if (this.value !== newPasswordInput.value) {
            showError("Паролі не співпадають");
        }
    });

    generatePasswordBtn.addEventListener('click', function () {
        const password = generateStrongPassword();
        newPasswordInput.value = password;
        confirmPasswordInput.value = password;
        newPasswordInput.dispatchEvent(new Event('input'));
        confirmPasswordInput.dispatchEvent(new Event('input'));
        confirmPasswordInput.disabled = false;
        saveButton.disabled = false;
    });

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

    form.addEventListener('submit', function (e) {
        if (newPasswordInput.value) {
            const strengthBar = document.getElementById('password-strength-bar');
            const backgroundColor = getComputedStyle(strengthBar).backgroundColor;

            if (backgroundColor === 'red' || backgroundColor === 'rgb(255, 0, 0)') {
                e.preventDefault();
                showError("Password is too weak. Please use at least a medium password.");
                return false;
            }
        }
    });

    function updatePasswordStrength(password) {
        const strengthBar = document.getElementById('password-strength-bar');
        const validation = validatePassword(password);

        if (!password) {
            strengthBar.style.width = '0%';
            strengthBar.style.backgroundColor = 'transparent';
            saveButton.disabled = false;
        } else if (validation.isLongEnough && validation.hasLowerCase &&
            validation.hasUpperCase && validation.hasDigits && validation.hasSpecial) {
            strengthBar.style.width = '100%';
            strengthBar.style.backgroundColor = 'green';
            saveButton.disabled = false;
        } else if (validation.isLongEnough && validation.hasLowerCase &&
            validation.hasUpperCase && validation.hasDigits) {
            strengthBar.style.width = '70%';
            strengthBar.style.backgroundColor = 'orange';
            saveButton.disabled = false;
        } else if (validation.isLongEnough) {
            strengthBar.style.width = '35%';
            strengthBar.style.backgroundColor = 'red';
            saveButton.disabled = true;
        } else {
            strengthBar.style.width = '0%';
            strengthBar.style.backgroundColor = 'transparent';
            saveButton.disabled = true;
        }
    }

    function showError(message) {
        errorMessage.innerText = message;
        errorMessage.classList.add('show');
        setTimeout(() => {
            errorMessage.classList.remove('show');
        }, 3000);
    }

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
});

document.addEventListener('DOMContentLoaded', function () {
    const form = document.querySelector('form');
    const saveButton = document.getElementById("saveChangesBtn");
    const inputs = Array.from(document.querySelectorAll("form input"));
    const newPasswordInput = document.querySelector("input[name='Password']");
    const confirmPasswordInput = document.querySelector("input[name='ConfirmPassword']");
    const usernameField = document.querySelector("input[name='Username']");
    const initialValues = inputs.reduce((acc, input) => {
        acc[input.name] = input.value;
        return acc;
    }, {});

    confirmPasswordInput.disabled = true;

    newPasswordInput.addEventListener("input", function () {
        confirmPasswordInput.disabled = !newPasswordInput.value;
    });

    inputs.forEach(input => {
        input.addEventListener("input", () => {
            const hasChanges = inputs.some(i => i.value !== initialValues[i.name]) ||
                newPasswordInput.value !== '' ||
                confirmPasswordInput.value !== '';
            saveButton.disabled = !hasChanges;
        });
    });

    usernameField.addEventListener('blur', function () {
        const username = this.value;

        if (username.trim() === initialValues['Username'] && username.trim() === '') {
            return;
        }

        fetch(`/Home/CheckUsernameAvailability?username=${encodeURIComponent(username)}`)
            .then(response => response.json())
            .then(data => {
                if (!data.isAvailable) {
                    alert('Таке ім\'я користувача вже зайняте');
                }
            })
        .catch(() => {
            alert('Помилка перевірки');
        });
    });
});

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll("form input").forEach(input => {
        input.addEventListener("contextmenu", event => {
            event.preventDefault();
        });
    });

    const nameFields = [
        "FirstName",
        "LastName",
        "MiddleName"
    ];

    nameFields.forEach(name => {
        const field = document.querySelector(`input[name='${name}']`);

        if (field) {
            field.addEventListener("input", () => {
                field.value = field.value.replace(/[^a-zA-Zа-яА-ЯіІїЇєЄґҐ\s]/g, "");
            });
        }
    });

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
});

document.addEventListener('DOMContentLoaded', function () {
    const commentsHeader = document.querySelector('.comments-header');
    const commentsContent = document.querySelector('.comments-content');

    if (commentsHeader && commentsContent) {
        commentsHeader.addEventListener('click', () => {
            commentsContent.classList.toggle('active');
            commentsHeader.classList.toggle('active');
        });
    }
});