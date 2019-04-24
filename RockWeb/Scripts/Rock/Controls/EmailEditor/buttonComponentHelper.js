﻿(function ($) {
    'use strict';
    window.Rock = window.Rock || {};
    Rock.controls = Rock.controls || {};
    Rock.controls.emailEditor = Rock.controls.emailEditor || {};
    Rock.controls.emailEditor.$currentButtonComponent = $(false);

    Rock.controls.emailEditor.buttonComponentHelper = (function () {
        var exports = {
            initializeEventHandlers: function () {
                var self = this;
                $('#component-button-linktype').on('change', function () {
                    self.setButtonLinkType();
                });

                $('#component-button-emailaddress').on('input', function () {
                    self.setButtonEmailAddress();
                });

                $('#component-button-emailsubject').on('input', function () {
                    self.setButtonEmailSubject();
                });

                $('#component-button-emailbody').on('input', function () {
                    self.setButtonEmailBody();
                });

                $('#component-button-buttonbackgroundcolor').colorpicker().on('changeColor', function () {
                    self.setButtonBackgroundColor();
                });

                $('#component-button-buttonfontcolor').colorpicker().on('changeColor', function () {
                    self.setButtonFontColor();
                });

                $('#component-button-buttontext').on('input', function (e) {
                    self.setButtonText();
                });

                $('#component-button-buttonurl').on('input', function (e) {
                    self.setButtonUrl();
                });

                $('#component-button-buttonwidth').on('change', function (e) {
                    self.setButtonWidth();
                });

                $('#component-button-buttonfixedwidth').on('change', function (e) {
                    self.setButtonWidth();
                });

                $('#component-button-buttonalign').on('change', function (e) {
                    self.setButtonAlign();
                });

                $('#component-button-buttonfont').on('change', function (e) {
                    self.setButtonFont();
                });

                $('#component-button-buttonfontweight').on('change', function (e) {
                    self.setButtonFontWeight();
                });

                $('#component-button-buttonfontsize').on('input', function (e) {
                    self.setButtonFontSize();
                });

                $('#component-button-buttonpadding').on('input', function (e) {
                    self.setButtonPadding();
                });
            },
            setProperties: function ($buttonComponent) {
                Rock.controls.emailEditor.$currentButtonComponent = $buttonComponent;

                //Set the value of the link type drop down
                if ($buttonComponent.attr('data-button-linktype')) {
                    $('#component-button-linktype').val($buttonComponent.attr('data-button-linktype'));
                }
                else {
                    $('#component-button-linktype').val('Url');
                }


                var buttonText = $buttonComponent.find('.button-link').text();
                var buttonBackgroundColor = $buttonComponent.find('.button-shell').css('backgroundColor');
                var buttonFontColor = $buttonComponent.find('.button-link').css('color');
                var buttonWidth = $buttonComponent.find('.button-shell').attr('width') || null;
                var buttonAlign = $buttonComponent.find('.button-innerwrap').attr('align');
                var buttonFont = $buttonComponent.find('.button-link').css("font-family");
                var buttonFontWeight = $buttonComponent.find('.button-link')[0].style['font-weight'];
                var buttonFontSize = $buttonComponent.find('.button-link').css("font-size");
                var buttonPadding = $buttonComponent.find('.button-content')[0].style['padding'];

                $('#component-button-buttontext').val(buttonText);
                $('#component-button-buttonbackgroundcolor').colorpicker('setValue', buttonBackgroundColor);
                $('#component-button-buttonfontcolor').colorpicker('setValue', buttonFontColor);

                var $buttonfixedwidthDiv = $('#component-button-panel').find('.js-buttonfixedwidth');

                if (buttonWidth == null) {
                    $('#component-button-buttonwidth').val(0);
                    $buttonfixedwidthDiv.hide();
                    $('#component-button-buttonfixedwidth').val('');
                }
                else if (buttonWidth == '100%') {
                    $('#component-button-buttonwidth').val(1);
                    $buttonfixedwidthDiv.hide();
                    $('#component-button-buttonfixedwidth').val('');
                }
                else {
                    $('#component-button-buttonwidth').val(2);
                    $buttonfixedwidthDiv.show();
                    $('#component-button-buttonfixedwidth').val(buttonWidth);
                }

                $('#component-button-buttonalign').val(buttonAlign);

                $('#component-button-buttonfont').val(buttonFont);
                $('#component-button-buttonfontweight').val(buttonFontWeight);
                $('#component-button-buttonfontsize').val(buttonFontSize);
                $('#component-button-buttonpadding').val(buttonPadding);

                this.setButtonLinkType();
            },
            setButtonLinkType: function () {
                var buttonComponent = Rock.controls.emailEditor.$currentButtonComponent;
                var linkType = $('#component-button-linktype').val();

                switch (linkType) {
                    case "Email":
                        //Save which type we are using on the component
                        buttonComponent.attr('data-button-linktype', 'Email');

                        //Set values
                        $('#component-button-emailaddress').val(buttonComponent.attr('data-button-emailaddress'));
                        $('#component-button-emailsubject').val(buttonComponent.attr('data-button-emailsubject'));
                        $('#component-button-emailbody').val(buttonComponent.attr('data-button-emailbody'));

                        //Hide the url and show the email
                        $('#component-button-urlfields').hide();
                        $('#componentButtonFile').hide();
                        $('#component-button-emailfields').show();


                        break;
                    case "File":
                        buttonComponent.attr('data-button-linktype', 'File');

                        //hide other options and show file
                        $('#component-button-urlfields').hide();
                        $('#component-button-emailfields').hide();
                        $('#componentButtonFile').show();
                        break;
                    default:
                        //Url is default
                        //Save which type we are using on the component
                        buttonComponent.attr('data-button-linktype', 'Url');

                        //Hide the email and show the url
                        $('#componentButtonFile').hide();
                        $('#component-button-emailfields').hide();
                        $('#component-button-urlfields').show();
                }
                this.setButtonUrl();
            },
            setButtonEmailAddress: function () {
                Rock.controls.emailEditor.$currentButtonComponent.attr('data-button-emailaddress', $('#component-button-emailaddress').val());
                this.setButtonUrl();
            },
            setButtonEmailSubject: function () {
                Rock.controls.emailEditor.$currentButtonComponent.attr('data-button-emailsubject', $('#component-button-emailsubject').val());
                this.setButtonUrl();
            },
            setButtonEmailBody: function () {
                Rock.controls.emailEditor.$currentButtonComponent.attr('data-button-emailbody', $('#component-button-emailbody').val());
                this.setButtonUrl();
            },
            setButtonText: function () {
                var text = $('#component-button-buttontext').val();
                Rock.controls.emailEditor.$currentButtonComponent.find('.button-link')
                    .text(text)
                    .attr('title', text);
            },
            setButtonUrl: function () {
                var buttonComponent = Rock.controls.emailEditor.$currentButtonComponent;
                var link = '';
                var linkType = $('#component-button-linktype').val();

                switch (linkType) {

                    case 'Email':
                        var address = $('#component-button-emailaddress').val();
                        var subject = $('#component-button-emailsubject').val();
                        var body = $('#component-button-emailbody').val();
                        link = 'mailto:' +
                            encodeURIComponent(address);
                        if (subject.length) {
                            link += '?subject=' + encodeURIComponent(subject);
                            if (body.length) {
                                link += '&body=' + encodeURIComponent(body);
                            }
                        }
                        buttonComponent.find('.button-link').attr('href', link);
                        break;
                    case 'File':
                        var fileId = buttonComponent.attr('data-button-file');
                        if (fileId) {
                            link = Rock.settings.get('baseUrl')
                                + 'GetFile.ashx?'
                                + '&id=' + fileId
                                + '&fileName=' + buttonComponent.attr('data-button-filename');
                        }
                        break;
                    default:
                        link = $('#component-button-buttonurl').val();
                        buttonComponent.attr('data-button-url', link);

                }

                buttonComponent.find('.button-link').attr('href', link);
            },
            setButtonBackgroundColor: function () {
                var color = $('#component-button-buttonbackgroundcolor').colorpicker('getValue');
                Rock.controls.emailEditor.$currentButtonComponent.find('.button-shell').css('backgroundColor', color);
            },
            setButtonFontColor: function () {
                var color = $('#component-button-buttonfontcolor').colorpicker('getValue');
                Rock.controls.emailEditor.$currentButtonComponent.find('.button-link').css('color', color);
            },
            setButtonWidth: function () {
                var selectValue = $('#component-button-buttonwidth').val();
                var fixedValue = $('#component-button-buttonfixedwidth').val();
                var $buttonfixedwidthDiv = $('#component-button-panel').find('.js-buttonfixedwidth');

                if (selectValue == 0) {
                    Rock.controls.emailEditor.$currentButtonComponent.find('.button-shell').removeAttr('width');
                    $buttonfixedwidthDiv.slideUp();
                }
                else if (selectValue == 1) {
                    Rock.controls.emailEditor.$currentButtonComponent.find('.button-shell').attr('width', '100%');
                    $buttonfixedwidthDiv.slideUp();
                }
                else if (selectValue == 2) {
                    Rock.controls.emailEditor.$currentButtonComponent.find('.button-shell').attr('width', fixedValue);
                    $buttonfixedwidthDiv.slideDown();
                }
            },
            setButtonAlign: function () {
                var selectValue = $('#component-button-buttonalign').val();
                Rock.controls.emailEditor.$currentButtonComponent.find('.button-innerwrap')
                    .attr('align', selectValue)
                    .css('text-align', selectValue);
            },
            setButtonFont: function () {
                var selectValue = $('#component-button-buttonfont').val();
                Rock.controls.emailEditor.$currentButtonComponent.find('.button-link').css('font-family', selectValue);
            },
            setButtonFontWeight: function () {
                var selectValue = $('#component-button-buttonfontweight').val();
                Rock.controls.emailEditor.$currentButtonComponent.find('.button-link').css('font-weight', selectValue);
            },
            setButtonFontSize: function () {
                var text = $('#component-button-buttonfontsize').val();
                Rock.controls.emailEditor.$currentButtonComponent.find('.button-link').css('font-size', text);
            },
            setButtonPadding: function () {
                var text = $('#component-button-buttonpadding').val();
                Rock.controls.emailEditor.$currentButtonComponent.find('.button-content').css('padding', text);
            },
            handleButtonFileUpdate: function (e, data) {
                Rock.controls.emailEditor.$currentButtonComponent.attr('data-button-file', data ? data.response().result.Id : null);
                Rock.controls.emailEditor.$currentButtonComponent.attr('data-button-filename', data ? data.response().result.FileName : null);
                this.setButtonUrl();
            }
        };
        return exports;

    }());
}(jQuery));

