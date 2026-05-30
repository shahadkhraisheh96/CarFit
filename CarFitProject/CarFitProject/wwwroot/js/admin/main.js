(function ($) {
    "use strict";

    // 1. Spinner Loading Dismissal Animation
    var spinner = function () {
        setTimeout(function () {
            if ($('#spinner').length > 0) {
                $('#spinner').removeClass('show');
            }
        }, 50);
    };
    spinner();


    // 2. Responsive Sticky Top Back-to-Top Button Utility Matrix
    $(window).scroll(function () {
        if ($(this).scrollTop() > 300) {
            $('.back-to-top').fadeIn('slow');
        } else {
            $('.back-to-top').fadeOut('slow');
        }
    });

    $('.back-to-top').click(function (e) {
        e.preventDefault(); // Safe alternative to return false
        $('html, body').animate({ scrollTop: 0 }, 1000, 'easeInOutExpo');
    });


    // 3. FIX: Sidebar Toggler Event Handling (Isolated from breaking links)
    $('.sidebar-toggler').click(function (e) {
        e.preventDefault(); // Prevents page reload without hijacking unrelated DOM form post submission layers
        $('.sidebar, .content').toggleClass("open");
    });


    // 4. Component Progress Bars UI Waypoint Animations
    if ($('.pg-bar').length > 0 && typeof $.fn.waypoint !== 'undefined') {
        $('.pg-bar').waypoint(function () {
            $('.progress .progress-bar').each(function () {
                $(this).css("width", $(this).attr("aria-valuenow") + '%');
            });
        }, { offset: '80%' });
    }


    // 5. Calendar Inline Widget Binding Validation Guardrail
    if ($('#calender').length > 0 && $.fn.datetimepicker) {
        $('#calender').datetimepicker({
            inline: true,
            format: 'L'
        });
    }


    // 6. Testimonial Feedback Carousel Slides Configuration
    if ($('.testimonial-carousel').length > 0 && $.fn.owlCarousel) {
        $(".testimonial-carousel").owlCarousel({
            autoplay: true,
            smartSpeed: 1000,
            items: 1,
            dots: true,
            loop: true,
            nav: false
        });
    }


    // 7. Global Analytics Chart UI Configurations
    if (typeof Chart !== 'undefined') {
        Chart.defaults.color = "#6C7293";
        Chart.defaults.borderColor = "rgba(255,255,255,0.1)";

        // Worldwide Sales Chart Rendering 
        if ($("#worldwide-sales").length > 0) {
            var ctx1 = $("#worldwide-sales").get(0).getContext("2d");
            var myChart1 = new Chart(ctx1, {
                type: "bar",
                data: {
                    labels: ["2016", "2017", "2018", "2019", "2020", "2021", "2022"],
                    datasets: [{
                        label: "USA",
                        data: [15, 30, 55, 65, 60, 80, 95],
                        backgroundColor: "rgba(235, 22, 22, .7)"
                    },
                    {
                        label: "UK",
                        data: [8, 35, 40, 60, 70, 55, 75],
                        backgroundColor: "rgba(235, 22, 22, .5)"
                    },
                    {
                        label: "AU",
                        data: [12, 25, 45, 55, 65, 70, 60],
                        backgroundColor: "rgba(235, 22, 22, .3)"
                    }
                    ]
                },
                options: { responsive: true }
            });
        }

        // Sales & Revenue Line Tracking Chart Renders
        if ($("#salse-revenue").length > 0) {
            var ctx2 = $("#salse-revenue").get(0).getContext("2d");
            var myChart2 = new Chart(ctx2, {
                type: "line",
                data: {
                    labels: ["2016", "2017", "2018", "2019", "2020", "2021", "2022"],
                    datasets: [{
                        label: "Sales",
                        data: [15, 30, 55, 45, 70, 65, 85],
                        backgroundColor: "rgba(235, 22, 22, .7)",
                        fill: true
                    },
                    {
                        label: "Revenue",
                        data: [99, 135, 170, 130, 190, 180, 270],
                        backgroundColor: "rgba(235, 22, 22, .5)",
                        fill: true
                    }
                    ]
                },
                options: { responsive: true }
            });
        }
    }

})(jQuery);