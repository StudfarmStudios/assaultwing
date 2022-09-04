videos = ["https://www.youtube.com/embed/MQ49vtHCnfY?controls=0", "https://www.youtube.com/embed/poxXPWh4nCU?controls=0"];
screenshot_amount = 10;

screenshot_page_width = 654;

$(document).ready(function() {
    for (var i = 0; i<videos.length; i++) {
        $('#videoNumbers').append('<a id="videoLink_'+i+'" class="videoLink" href="javascript: void(0);" onclick="selectVideo('+i+')">'+(i+1)+'</a>');
    }

    selectVideo(0);

    var screenshot_pages = Math.ceil(screenshot_amount / 4);
    for (var i = 0;i<screenshot_pages; i++) {
        if (i == 0) {
            $('#ssNumbers').append('<a id="ssLink_'+i+'" class="ssLink videoLink videoSelected" data-page="'+(i+1)+'" href="javascript: void(0);" onclick="selectSsPage('+i+')">'+(i+1)+'</a>');
            $('#selectedSSPage').val(i+'');
        } else {
            $('#ssNumbers').append('<a id="ssLink_'+i+'" class="ssLink videoLink" data-page="'+(i+1)+'" href="javascript: void(0);" onclick="selectSsPage('+i+')">'+(i+1)+'</a>');
        }
    }

    var html = '<li>';
    for (var i = 0; i<screenshot_amount; i++) {

        html += '<a href="images/screenshots/ss_'+(i+1)+'.jpg"><img class="ssThumb" src="images/screenshots/ss_'+(i+1)+'_thumb.jpg" alt="" /></a>';
        if ((i+1) % 4 == 0 && i > 0 && i < 8) {
            html += '</li><li>';
        } else if (i == 9) {
            html += '</li>';
        }
    }

    $('#ssEmbed').append(html);

    $('#ssEmbed').anythingSlider({
        autoPlay: false,
        buildNavigation: false
    });

    $('.ssLink').each(function(index, value) {
        $(value).unbind('click');
        $(value).click(function() {
            $('.ssLink').removeClass('videoSelected');
            $('#ssLink_'+($(value).data('page')-1)).addClass('videoSelected');
            $('#ssEmbed').anythingSlider($(value).data('page'));
        })
    });

    $('#ssEmbed a').lightBox({
        keyToClose: 'esc'
    });

    $('#videoArrowRight').click(function() {
        var selectedVideo = $('#selectedVideoUrl').val();
        $('.videoLink').removeClass('videoSelected');
        for (var i = 0; i<videos.length; i++) {
            if (selectedVideo == videos[i]) {
                if (i == 0) {
                    selectVideo(videos.length - 1)
                } else {
                    selectVideo(i - 1)
                }
            }
        }
    });

    $('#videoArrowLeft').click(function() {
        var selectedVideo = $('#selectedVideoUrl').val();
        $('.videoLink').removeClass('videoSelected');
        for (var i = 0; i<videos.length; i++) {
            if (selectedVideo == videos[i]) {
                if (i == videos.length-1) {
                    selectVideo(0)
                } else {
                    selectVideo(i+1)
                }
            }
        }
    });

    $('#ssArrowLeft').click(function() {
        var selectedPage = parseInt($('#selectedSSPage').val());
        $('.ssLink').removeClass('videoSelected');
        if (selectedPage < 2) {
            var newPage = selectedPage + 1;
        } else {
            var newPage = 0;
        }
        $('#selectedSSPage').val(newPage);
        $('#ssLink_'+newPage).addClass('videoSelected');

        $('#ssEmbed').data('AnythingSlider').goForward();
    });

    $('#ssArrowRight').click(function() {
        var selectedPage = parseInt($('#selectedSSPage').val());
        $('.ssLink').removeClass('videoSelected');
        if (selectedPage == 0) {
            var newPage = 2;
        } else {
            var newPage = selectedPage-1;
        }

        $('#selectedSSPage').val(newPage);
        $('#ssLink_'+newPage).addClass('videoSelected');

        $('#ssEmbed').data('AnythingSlider').goBack();
    });

});

function selectVideo(item) {
    if (!isNaN(item)) {
        $('#videoEmbed').html('<iframe width="535" height="325" type="application/x-shockwave-flash" src="' + 
            videos[item] + 
            '" title="YouTube video player" frameborder="0" ' + 
            'allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>');
        $('.videoLink').removeClass('videoSelected');
        $('#videoLink_'+item).addClass('videoSelected');
        $('#selectedVideoUrl').val(videos[item]);
    }
}

function selectSsPage(item) {
    if (!isNaN(item)) {
        
    }
}
