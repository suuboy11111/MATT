// Blog Modern JavaScript

document.addEventListener('DOMContentLoaded', function () {
    // Check if jQuery is available
    if (typeof jQuery === 'undefined') {
        console.error('jQuery is not loaded');
        return;
    }

    const $ = jQuery;
    // Image Preview
    $('#imageInput').on('change', function () {
        const fileName = $(this)[0].files[0]?.name || '';
        $('#imageFileName').text(fileName ? '📷 ' + fileName : '');
    });

    // Like/Unlike Post
    $('.blog-like-btn').on('click', function (e) {
        e.preventDefault();
        const btn = $(this);
        const postId = btn.data('post-id');
        const icon = btn.find('i');

        // Disable button during request
        btn.prop('disabled', true);

        $.ajax({
            url: '/Blog/ToggleLike',
            type: 'POST',
            data: { blogPostId: postId },
            success: function (response) {
                if (response.success) {
                    // Update like button
                    if (response.isLiked) {
                        btn.addClass('liked');
                        icon.removeClass('far').addClass('fas');
                    } else {
                        btn.removeClass('liked');
                        icon.removeClass('fas').addClass('far');
                    }

                    // Update like count
                    $('#likeCount-' + postId).text(response.likeCount);
                } else {
                    alert(response.message || 'Có lỗi xảy ra');
                }
            },
            error: function () {
                alert('Có lỗi xảy ra khi thích bài viết');
            },
            complete: function () {
                btn.prop('disabled', false);
            }
        });
    });

    // Add Comment
    $('.blog-comment-form').on('submit', function (e) {
        e.preventDefault();
        const form = $(this);
        const postId = form.data('post-id');
        const input = form.find('.blog-comment-input');
        const content = input.val().trim();

        if (!content) {
            return;
        }

        // Disable form during request
        form.find('button').prop('disabled', true);
        const submitBtn = form.find('button');
        const originalHtml = submitBtn.html();
        submitBtn.html('<i class="fas fa-spinner fa-spin"></i>');

        $.ajax({
            url: '/Blog/AddComment',
            type: 'POST',
            data: {
                blogPostId: postId,
                content: content
            },
            success: function (response) {
                if (response.success) {
                    // Clear input
                    input.val('');

                    // Add comment to DOM
                    const commentsSection = $('#comments-' + postId);
                    const commentHtml = `
                        <div class="blog-comment-item">
                            <img src="${response.comment.authorAvatar}" 
                                 alt="${response.comment.authorName}" 
                                 class="blog-comment-avatar" 
                                 onerror="this.src='/images/default1-avatar.png'" />
                            <div class="blog-comment-content">
                                <strong>${escapeHtml(response.comment.authorName)}</strong>
                                <span>${escapeHtml(response.comment.content)}</span>
                                <small class="text-muted">${response.comment.createdAt}</small>
                            </div>
                        </div>
                    `;
                    commentsSection.prepend(commentHtml);

                    // Scroll to new comment
                    commentsSection.scrollTop(0);
                } else {
                    alert(response.message || 'Có lỗi xảy ra');
                }
            },
            error: function () {
                alert('Có lỗi xảy ra khi thêm bình luận');
            },
            complete: function () {
                form.find('button').prop('disabled', false);
                submitBtn.html(originalHtml);
            }
        });
    });

    // Load More Posts
    let isLoading = false;
    let pageNumber = 1;

    $('#loadMoreBtn').on('click', function () {
        if (isLoading) return;

        isLoading = true;
        const btn = $(this);
        const originalHtml = btn.html();
        btn.html('<i class="fas fa-spinner fa-spin me-2"></i>Đang tải...');
        btn.prop('disabled', true);

        pageNumber++;

        $.ajax({
            url: '/Blog/LoadMorePosts',
            type: 'GET',
            data: { page: pageNumber },
            success: function (data) {
                if (data.trim()) {
                    $('#blogFeed').append(data);
                    // Re-initialize like buttons for new posts
                    initializeNewPosts();
                } else {
                    btn.hide();
                }
            },
            error: function () {
                alert('Có lỗi xảy ra khi tải thêm bài viết');
            },
            complete: function () {
                isLoading = false;
                btn.html(originalHtml);
                btn.prop('disabled', false);
            }
        });
    });

    // Initialize like buttons for new posts
    function initializeNewPosts() {
        $('.blog-like-btn').off('click').on('click', function (e) {
            e.preventDefault();
            const btn = $(this);
            const postId = btn.data('post-id');
            const icon = btn.find('i');

            btn.prop('disabled', true);

            $.ajax({
                url: '/Blog/ToggleLike',
                type: 'POST',
                data: { blogPostId: postId },
                success: function (response) {
                    if (response.success) {
                        if (response.isLiked) {
                            btn.addClass('liked');
                            icon.removeClass('far').addClass('fas');
                        } else {
                            btn.removeClass('liked');
                            icon.removeClass('fas').addClass('far');
                        }
                        $('#likeCount-' + postId).text(response.likeCount);
                    }
                },
                error: function () {
                    alert('Có lỗi xảy ra khi thích bài viết');
                },
                complete: function () {
                    btn.prop('disabled', false);
                }
            });
        });
    }

    // Escape HTML to prevent XSS
    function escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, function (m) { return map[m]; });
    }
});

