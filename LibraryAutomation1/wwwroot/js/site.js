// Kütüphane Yönetim Sistemi - JavaScript

$(document).ready(function () {
    // Initialize tooltips
    $('[data-bs-toggle="tooltip"]').tooltip();

    // Initialize popovers
    $('[data-bs-toggle="popover"]').popover();

    // Auto-hide alerts after 5 seconds
    $('.alert').delay(5000).fadeOut('slow');

    // Add fade-in animation to cards
    $('.card').addClass('fade-in');

    // Table row hover effects
    $('.table tbody tr').hover(
        function () {
            $(this).addClass('table-active');
        },
        function () {
            $(this).removeClass('table-active');
        }
    );

    // Confirm delete operations
    $('a[href*="Delete"], button[formaction*="Delete"]').click(function (e) {
        if (!confirm('Bu işlemi gerçekleştirmek istediğinizden emin misiniz?')) {
            e.preventDefault();
            return false;
        }
    });

    // Auto-refresh dashboard stats
    if (window.location.pathname === '/' || window.location.pathname === '/Home') {
        setInterval(refreshDashboardStats, 300000); // 5 minutes
    }

    // Search functionality
    initializeSearch();

    // Rating system
    initializeRatingSystem();

    // Form validation enhancements
    enhanceFormValidation();

    // Loading states
    initializeLoadingStates();
});

// Dashboard stats refresh
function refreshDashboardStats() {
    $.get('/Home/GetDashboardStats', function (data) {
        // Update stats if the elements exist
        updateStatCard('total-books', data.totalBooks);
        updateStatCard('available-books', data.availableBooks);
        updateStatCard('total-members', data.totalMembers);
        updateStatCard('active-loans', data.activeLoans);
        updateStatCard('overdue-loans', data.overdueLoans);
        updateStatCard('total-ratings', data.totalRatings);
    }).fail(function () {
        console.log('Dashboard stats refresh failed');
    });
}

function updateStatCard(elementId, value) {
    const element = $('#' + elementId);
    if (element.length && element.text() !== value.toString()) {
        element.fadeOut(200, function () {
            $(this).text(value).fadeIn(200);
        });
    }
}

// Search functionality
function initializeSearch() {
    // Global search
    $('#global-search').on('input', debounce(function () {
        const query = $(this).val();
        if (query.length >= 3) {
            performSearch(query);
        } else {
            clearSearchResults();
        }
    }, 300));

    // Advanced search filters
    $('.search-filter').on('change', function () {
        applySearchFilters();
    });
}

function performSearch(query) {
    $.get('/Home/Search', { query: query }, function (data) {
        displaySearchResults(data);
    });
}

function clearSearchResults() {
    $('#search-results').empty();
}

function displaySearchResults(data) {
    // Implementation for displaying search results
    console.log('Search results:', data);
}

function applySearchFilters() {
    const filters = {};
    $('.search-filter').each(function () {
        const name = $(this).attr('name');
        const value = $(this).val();
        if (value) {
            filters[name] = value;
        }
    });

    // Apply filters to current page
    const currentUrl = new URL(window.location);
    Object.keys(filters).forEach(key => {
        currentUrl.searchParams.set(key, filters[key]);
    });

    window.location.href = currentUrl.toString();
}

// Rating system
function initializeRatingSystem() {
    // Star rating hover effects
    $('.rating-input label').hover(
        function () {
            const rating = $(this).prev('input').val();
            highlightStars($(this).closest('.rating-input'), rating);
        },
        function () {
            const checkedRating = $(this).closest('.rating-input').find('input:checked').val();
            if (checkedRating) {
                highlightStars($(this).closest('.rating-input'), checkedRating);
            } else {
                clearStarHighlight($(this).closest('.rating-input'));
            }
        }
    );

    // Star rating click
    $('.rating-input input[type="radio"]').change(function () {
        const rating = $(this).val();
        highlightStars($(this).closest('.rating-input'), rating);
    });
}

function highlightStars(container, rating) {
    container.find('label').each(function (index) {
        const starRating = container.find('input').eq(index).val();
        if (starRating <= rating) {
            $(this).find('i').removeClass('far').addClass('fas').css('color', '#ffc107');
        } else {
            $(this).find('i').removeClass('fas').addClass('far').css('color', '#ddd');
        }
    });
}

function clearStarHighlight(container) {
    container.find('label i').removeClass('fas').addClass('far').css('color', '#ddd');
}

// Form validation enhancements
function enhanceFormValidation() {
    // Real-time validation
    $('input[type="email"]').on('blur', function () {
        validateEmail($(this));
    });

    $('input[type="tel"]').on('input', function () {
        formatPhoneNumber($(this));
    });

    $('input[data-validation="isbn"]').on('input', function () {
        validateISBN($(this));
    });

    // Form submission validation
    $('form').on('submit', function (e) {
        if (!validateForm($(this))) {
            e.preventDefault();
            return false;
        }
    });
}

function validateEmail(input) {
    const email = input.val();
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    if (email && !emailRegex.test(email)) {
        input.addClass('is-invalid');
        showFieldError(input, 'Geçerli bir e-posta adresi giriniz.');
    } else {
        input.removeClass('is-invalid').addClass('is-valid');
        hideFieldError(input);
    }
}

function formatPhoneNumber(input) {
    let value = input.val().replace(/\D/g, '');
    if (value.length >= 10) {
        value = value.replace(/(\d{4})(\d{3})(\d{4})/, '$1-$2-$3');
        input.val(value);
    }
}

function validateISBN(input) {
    const isbn = input.val().replace(/[^0-9X]/gi, '');
    const isValid = isbn.length === 10 || isbn.length === 13;

    if (isbn.length > 0 && !isValid) {
        input.addClass('is-invalid');
        showFieldError(input, 'ISBN 10 veya 13 haneli olmalıdır.');
    } else {
        input.removeClass('is-invalid');
        if (isbn.length > 0) {
            input.addClass('is-valid');
        }
        hideFieldError(input);
    }
}

function validateForm(form) {
    let isValid = true;

    // Check required fields
    form.find('[required]').each(function () {
        if (!$(this).val().trim()) {
            $(this).addClass('is-invalid');
            showFieldError($(this), 'Bu alan zorunludur.');
            isValid = false;
        }
    });

    // Check invalid fields
    if (form.find('.is-invalid').length > 0) {
        isValid = false;
    }

    return isValid;
}

function showFieldError(input, message) {
    hideFieldError(input);
    const errorDiv = $('<div class="invalid-feedback">' + message + '</div>');
    input.after(errorDiv);
}

function hideFieldError(input) {
    input.next('.invalid-feedback').remove();
}

// Loading states
function initializeLoadingStates() {
    // Show loading on form submissions
    $('form').on('submit', function () {
        const submitBtn = $(this).find('button[type="submit"], input[type="submit"]');
        showLoading(submitBtn);
    });

    // Show loading on AJAX requests
    $(document).ajaxStart(function () {
        showGlobalLoading();
    }).ajaxStop(function () {
        hideGlobalLoading();
    });
}

function showLoading(element) {
    const originalText = element.text();
    element.data('original-text', originalText);
    element.prop('disabled', true);
    element.html('<i class="fas fa-spinner fa-spin"></i> Yükleniyor...');
}

function hideLoading(element) {
    const originalText = element.data('original-text');
    element.prop('disabled', false);
    element.html(originalText);
}

function showGlobalLoading() {
    if ($('#global-loading').length === 0) {
        $('body').append('<div id="global-loading" class="position-fixed top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center" style="background: rgba(0,0,0,0.5); z-index: 9999;"><div class="spinner"></div></div>');
    }
}

function hideGlobalLoading() {
    $('#global-loading').remove();
}

// Utility functions
function debounce(func, wait, immediate) {
    let timeout;
    return function executedFunction() {
        const context = this;
        const args = arguments;
        const later = function () {
            timeout = null;
            if (!immediate) func.apply(context, args);
        };
        const callNow = immediate && !timeout;
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
        if (callNow) func.apply(context, args);
    };
}

function formatCurrency(amount) {
    return new Intl.NumberFormat('tr-TR', {
        style: 'currency',
        currency: 'TRY'
    }).format(amount);
}

function formatDate(date) {
    return new Intl.DateTimeFormat('tr-TR').format(new Date(date));
}

function showNotification(message, type = 'info') {
    const alertClass = `alert-${type}`;
    const iconClass = {
        'success': 'fa-check-circle',
        'error': 'fa-exclamation-circle',
        'warning': 'fa-exclamation-triangle',
        'info': 'fa-info-circle'
    }[type] || 'fa-info-circle';

    const notification = $(`
        <div class="alert ${alertClass} alert-dismissible fade show position-fixed" 
             style="top: 20px; right: 20px; z-index: 1050; min-width: 300px;">
            <i class="fas ${iconClass}"></i> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `);

    $('body').append(notification);

    // Auto-hide after 5 seconds
    setTimeout(() => {
        notification.alert('close');
    }, 5000);
}

// Export functions for global use
window.LibrarySystem = {
    showNotification,
    showLoading,
    hideLoading,
    formatCurrency,
    formatDate,
    validateForm
};
