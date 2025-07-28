// D'CINE System JavaScript

// Global functions
window.DCineApp = {
    // Format currency
    formatCurrency: function(amount) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount);
    },

    // Format date
    formatDate: function(date) {
        return new Date(date).toLocaleDateString('vi-VN');
    },

    // Format time
    formatTime: function(date) {
        return new Date(date).toLocaleTimeString('vi-VN', {
            hour: '2-digit',
            minute: '2-digit'
        });
    },

    // Show loading
    showLoading: function(element) {
        var $el = $(element);
        $el.html('<div class="text-center py-3"><div class="spinner-border text-primary" role="status"></div></div>');
    },

    // Show error
    showError: function(element, message) {
        var $el = $(element);
        $el.html('<div class="alert alert-danger text-center"><i class="fas fa-exclamation-triangle me-2"></i>' + message + '</div>');
    },

    // Show success message
    showSuccess: function(message) {
        var alertHtml = '<div class="alert alert-success alert-dismissible fade show" role="alert">' +
            '<i class="fas fa-check-circle me-2"></i>' + message +
            '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>' +
            '</div>';
        
        $('main').prepend(alertHtml);
        
        // Auto dismiss after 5 seconds
        setTimeout(function() {
            $('.alert-success').alert('close');
        }, 5000);
    },

    // Confirm dialog
    confirm: function(message, callback) {
        if (confirm(message)) {
            callback();
        }
    },

    // Validate form
    validateForm: function(formSelector) {
        var isValid = true;
        $(formSelector + ' [required]').each(function() {
            if (!$(this).val()) {
                $(this).addClass('is-invalid');
                isValid = false;
            } else {
                $(this).removeClass('is-invalid');
            }
        });
        return isValid;
    }
};

// Document ready
$(document).ready(function() {
    // Add fade-in animation to cards
    $('.card').each(function(index) {
        $(this).delay(index * 100).queue(function() {
            $(this).addClass('fade-in').dequeue();
        });
    });

    // Auto-hide alerts after 5 seconds
    setTimeout(function() {
        $('.alert:not(.alert-permanent)').alert('close');
    }, 5000);

    // Tooltip initialization
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Form validation
    $('form').on('submit', function(e) {
        var $form = $(this);
        var isValid = true;

        $form.find('[required]').each(function() {
            if (!$(this).val()) {
                $(this).addClass('is-invalid');
                isValid = false;
            } else {
                $(this).removeClass('is-invalid');
            }
        });

        if (!isValid) {
            e.preventDefault();
        }
    });

    // Remove validation errors on input
    $('input, select, textarea').on('input change', function() {
        $(this).removeClass('is-invalid');
    });

    // Loading button state
    $('.btn[data-loading-text]').on('click', function() {
        var $btn = $(this);
        var loadingText = $btn.data('loading-text');
        var originalText = $btn.html();
        
        $btn.html(loadingText).prop('disabled', true);
        
        // Reset after 10 seconds (fallback)
        setTimeout(function() {
            $btn.html(originalText).prop('disabled', false);
        }, 10000);
    });
});

// Global error handler for AJAX
$(document).ajaxError(function(event, xhr, settings, error) {
    console.error('AJAX Error:', error);
    
    if (xhr.status === 401) {
        alert('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
        window.location.href = '/Auth/Login';
    } else if (xhr.status === 500) {
        alert('Có lỗi hệ thống xảy ra. Vui lòng thử lại sau.');
    } else if (xhr.status === 0) {
        alert('Không thể kết nối đến server. Vui lòng kiểm tra kết nối mạng.');
    }
});

// Prevent double submission
$('form').on('submit', function() {
    var $form = $(this);
    var $submitBtn = $form.find('button[type="submit"], input[type="submit"]');
    
    setTimeout(function() {
        $submitBtn.prop('disabled', true);
    }, 100);
    
    setTimeout(function() {
        $submitBtn.prop('disabled', false);
    }, 3000);
});

// Rating System
document.addEventListener('DOMContentLoaded', function() {
    // Initialize rating system
    initializeRatingSystem();
    
    // Initialize rating display
    initializeRatingDisplay();
});

function initializeRatingSystem() {
    const ratingContainers = document.querySelectorAll('.rating-container');
    
    ratingContainers.forEach(container => {
        const radioButtons = container.querySelectorAll('input[type="radio"]');
        const ratingNumbers = container.querySelectorAll('.rating-number');
        
        // Add click event to rating numbers
        ratingNumbers.forEach((number, index) => {
            number.addEventListener('click', function() {
                const selectedValue = index + 1; // 1 is first, 10 is last
                const radio = container.querySelector(`input[type="radio"][value="${selectedValue}"]`);
                
                if (radio) {
                    radio.checked = true;
                    
                    // Update rating text
                    updateRatingText(container, selectedValue);
                    
                    // Trigger change event
                    radio.dispatchEvent(new Event('change'));
                }
            });
        });
        
        // Add change event to radio buttons
        radioButtons.forEach((radio) => {
            radio.addEventListener('change', function() {
                const value = parseInt(radio.value);
                updateRatingText(container, value);
            });
        });
        
        // Initialize with current value
        const checkedRadio = container.querySelector('input[type="radio"]:checked');
        if (checkedRadio) {
            const value = parseInt(checkedRadio.value);
            updateRatingText(container, value);
        }
    });
}



function updateRatingText(container, selectedRating) {
    const ratingText = container.querySelector('.rating-text small');
    if (ratingText) {
        ratingText.textContent = `Chọn ${selectedRating}/10 điểm`;
    }
}

function initializeRatingDisplay() {
    const ratingDisplays = document.querySelectorAll('.rating-display');
    
    ratingDisplays.forEach(display => {
        const rating = parseFloat(display.dataset.rating || 0);
        const starsContainer = display.querySelector('.stars');
        
        if (starsContainer) {
            starsContainer.innerHTML = generateStars(rating);
        }
    });
}

function generateStars(rating) {
    const fullStars = Math.floor(rating);
    const hasHalfStar = rating % 1 !== 0;
    const emptyStars = 10 - fullStars - (hasHalfStar ? 1 : 0);
    
    let starsHTML = '';
    
    // Full stars
    for (let i = 0; i < fullStars; i++) {
        starsHTML += '<i class="fas fa-star"></i>';
    }
    
    // Half star
    if (hasHalfStar) {
        starsHTML += '<i class="fas fa-star-half-alt"></i>';
    }
    
    // Empty stars
    for (let i = 0; i < emptyStars; i++) {
        starsHTML += '<i class="far fa-star"></i>';
    }
    
    return starsHTML;
}

// Function to get selected rating value
function getSelectedRating(containerId) {
    const container = document.getElementById(containerId);
    if (!container) return 0;
    
    const selectedRadio = container.querySelector('input[type="radio"]:checked');
    return selectedRadio ? parseInt(selectedRadio.value) : 0;
}

// Function to set rating value
function setRating(containerId, rating) {
    const container = document.getElementById(containerId);
    if (!container) return;
    
    const radio = container.querySelector(`input[type="radio"][value="${rating}"]`);
    if (radio) {
        radio.checked = true;
        updateRatingStars(container, rating);
    }
}