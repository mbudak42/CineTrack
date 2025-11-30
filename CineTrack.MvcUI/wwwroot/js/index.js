let currentActivityId = 0;

// 1. Beğeni İşlemi
function toggleLike(btn, activityId) {
	// UI Anında Tepki (Optimistic Update)
	const icon = btn.querySelector('i');
	const span = btn.querySelector('span');
	let count = parseInt(span.innerText);

	const isLiked = icon.classList.contains('bi-heart-fill');

	if (isLiked) {
		icon.classList.replace('bi-heart-fill', 'bi-heart');
		btn.classList.replace('text-danger', 'text-muted');
		span.innerText = Math.max(0, count - 1);
	} else {
		icon.classList.replace('bi-heart', 'bi-heart-fill');
		btn.classList.replace('text-muted', 'text-danger');
		span.innerText = count + 1;
	}

	// Backend İsteği
	fetch(`/Feed/ToggleLike/${activityId}`, { method: 'POST' })
		.then(res => res.json())
		.then(data => {
			// Gerekirse sunucudan gelen kesin veriyle düzelt
		})
		.catch(err => console.error(err));
}

// 2. Yorumları Aç
function openCommentsModal(activityId) {
	currentActivityId = activityId;
	const modal = new bootstrap.Modal(document.getElementById('commentsModal'));
	const container = document.getElementById('commentsList');

	container.innerHTML = '<div class="text-center"><div class="spinner-border text-primary"></div></div>';
	modal.show();

	fetch(`/Feed/GetComments/${activityId}`)
		.then(res => res.json())
		.then(data => {
			container.innerHTML = '';
			if (!data || data.length === 0) {
				container.innerHTML = '<p class="text-muted text-center">Henüz yorum yok. İlk yorumu sen yap!</p>';
				return;
			}

			data.forEach(c => {
				const html = `
                    <div class="d-flex gap-2">
                        <img src="${c.userAvatar || '/images/none.png'}" class="rounded-circle" style="width:32px;height:32px;">
                        <div class="bg-light p-2 rounded flex-grow-1">
                            <div class="fw-bold small">${c.username}</div>
                            <div class="text-dark small">${c.text}</div>
                            <div class="text-muted " style="font-size:0.7rem">${new Date(c.createdAt).toLocaleString()}</div>
                        </div>
                    </div>
                `;
				container.innerHTML += html;
			});
		});
}

// 3. Yorum Gönder
function submitComment() {
	const input = document.getElementById('commentInput');
	const text = input.value;
	if (!text.trim()) return;

	const formData = new FormData();
	formData.append('text', text);

	fetch(`/Feed/AddComment/${currentActivityId}`, {
		method: 'POST',
		body: formData
	})
		.then(res => res.json())
		.then(newComment => {
			input.value = '';
			// Modalı kapatıp açmak yerine listeye ekleyebiliriz veya modalı yenileyebiliriz.
			// Basitlik için listeye append edelim:
			const container = document.getElementById('commentsList');
			// Eğer boş mesajı varsa temizle
			if (container.innerText.includes('Henüz yorum yok')) container.innerHTML = '';

			const html = `
            <div class="d-flex gap-2">
                <img src="${newComment.userAvatar || '/images/none.png'}" class="rounded-circle" style="width:32px;height:32px;">
                <div class="bg-light p-2 rounded flex-grow-1">
                    <div class="fw-bold small">${newComment.username}</div>
                    <div class="text-dark small">${newComment.text}</div>
                    <div class="text-muted " style="font-size:0.7rem">Şimdi</div>
                </div>
            </div>
        `;
			container.innerHTML += html;
		});
}