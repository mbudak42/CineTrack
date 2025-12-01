// Puanlama Modalını Aç
	function openRatingModal() {
		var modal = new bootstrap.Modal(document.getElementById('ratingModal'));

		// Eğer kullanıcı daha önce puan vermişse o puanı seçili getir
		var currentRating = @(Model.CurrentUserRating);
		if (currentRating > 0) {
			var radio = document.getElementById('star' + currentRating);
			if (radio) radio.checked = true;
		}

		modal.show();
	}

	// Puanı Gönder ve Listeye Ekle
	function submitRatingWithList() {
		var ratingInput = document.querySelector('input[name="rating"]:checked');
		if (!ratingInput) {
			alert("Lütfen en az 1 yıldız seçin.");
			return;
		}

		var ratingValue = ratingInput.value;
		var contentId = '@Model.Id';
		// İçerik türüne göre varsayılan listeyi belirle
		var listName = '@(Model.ContentType == "movie" ? "izlediklerim" : "okuduklarim")';

		// 1. Puanı API'ye Gönder
		var formData = new FormData();
		formData.append('contentId', contentId);
		formData.append('rating', ratingValue);

		fetch('/Content/RateContent', {
			method: 'POST',
			body: formData
		})
			.then(res => res.json())
			.then(data => {
				if (data.success) {
					// 2. Başarılıysa Listeye Ekle (Mevcut fonksiyonu kullanıyoruz)
					// Bu fonksiyon arkada fetch atar, kullanıcıya alert gösterir.
					// Kullanıcı deneyimini kesmemek için alert'i bastırabilir veya
					// addToList fonksiyonunu biraz modifiye edebiliriz.
					// Şimdilik standart akışta çağırıyoruz:

					// Listeye ekleme işlemi (Arka planda)
					var listData = new FormData();
					listData.append('contentId', contentId);
					listData.append('listName', listName);

					fetch('/Content/AddToList', { method: 'POST', body: listData })
						.then(() => {
							// Her şey bitti, sayfayı yenile ki yeni puan görünsün
							location.reload();
						});

				} else {
					alert(data.message);
				}
			})
			.catch(err => {
				console.error(err);
				alert("Bir hata oluştu.");
			});
	}

	// --- MEVCUT FONKSİYONLAR ---

	function openCustomListModal() {
		var modal = new bootstrap.Modal(document.getElementById('addCustomListModal'));
		modal.show();

		document.getElementById('listsContainer').innerHTML = '';
		document.getElementById('loadingLists').style.display = 'block';

		fetch('/UserList/GetMyLists')
			.then(res => res.json())
			.then(lists => {
				document.getElementById('loadingLists').style.display = 'none';
				var container = document.getElementById('listsContainer');

				if (lists.length === 0) {
					container.innerHTML = '<div class="alert alert-warning">Henüz özel bir listeniz yok. Profilinizden oluşturabilirsiniz.</div>';
					return;
				}

				var customLists = lists.filter(l =>
					l.name !== 'izlediklerim' &&
					l.name !== 'izlenecekler' &&
					l.name !== 'okuduklarim' &&
					l.name !== 'okunacaklar'
				);

				if (customLists.length === 0) {
					container.innerHTML = '<div class="alert alert-info">Sadece varsayılan listeleriniz var. Yeni bir özel liste oluşturun.</div>';
					return;
				}

				customLists.forEach(list => {
					var btn = document.createElement('button');
					btn.className = 'list-group-item list-group-item-action d-flex justify-content-between align-items-center';
					btn.innerHTML = `<span>${list.name}</span> <span class="badge bg-secondary rounded-pill">${list.contents ? list.contents.length : 0}</span>`;
					btn.onclick = function () { addToCustomList(list.id); };
					container.appendChild(btn);
				});
			});
	}

	function addToCustomList(listId) {
		var contentId = '@Model.Id';
		var formData = new FormData();
		formData.append('listId', listId);
		formData.append('contentId', contentId);

		fetch('/UserList/AddContent', {
			method: 'POST',
			body: formData
		})
			.then(res => res.json())
			.then(data => {
				alert(data.message);
				var modalEl = document.getElementById('addCustomListModal');
				var modal = bootstrap.Modal.getInstance(modalEl);
				modal.hide();
			});
	}

	function addToList(contentId, listName) {
		const formData = new FormData();
		formData.append('contentId', contentId);
		formData.append('listName', listName);

		fetch('/Content/AddToList', {
			method: 'POST',
			body: formData
		})
			.then(response => response.json())
			.then(data => {
				// Eğer manuel butonla tıklandıysa (modal dışı) alert göster
				// Modal içinden çağrıldığında location.reload olacağı için bu alert görünmeyebilir
				alert(data.message);
			})
			.catch(error => {
				console.error('Error:', error);
				alert("Bir hata oluştu.");
			});
	}

	function submitComment(id, type) {
		var commentText = document.getElementById("newComment").value;
		if (!commentText) {
			alert("Lütfen bir yorum yazın.");
			return;
		}

		var formData = new FormData();
		formData.append('contentId', id);
		formData.append('text', commentText);

		fetch('/Content/AddComment', {
			method: 'POST',
			body: formData
		})
			.then(res => res.json())
			.then(data => {
				if (data.success) {
					// Başarılıysa sayfayı yenile ki yeni yorum ve aktivite görünsün
					location.reload();
				} else {
					alert(data.message);
				}
			})
			.catch(err => {
				console.error(err);
				alert("Bir hata oluştu.");
			});
	}

	// Modal'ı aç
	function openReviewModal() {
		var modal = new bootstrap.Modal(document.getElementById('reviewModal'));
		modal.show();
	}

	// Yorum Gönder / Güncelle
	function submitReviewFromModal() {
		var text = document.getElementById("modalReviewText").value;
		var contentId = '@Model.Id';

		if (!text.trim()) {
			alert("Lütfen bir şeyler yazın.");
			return;
		}

		var formData = new FormData();
		formData.append('contentId', contentId);
		formData.append('text', text);

		fetch('/Content/AddComment', { // Mevcut AddComment metodu hem ekleme hem güncelleme yapıyor
			method: 'POST',
			body: formData
		})
			.then(res => res.json())
			.then(data => {
				if (data.success) {
					location.reload();
				} else {
					alert(data.message);
				}
			})
			.catch(err => console.error(err));
	}

	// Yorum Sil
	function deleteReview() {
		if (!confirm("Yorumunuzu silmek istediğinize emin misiniz?")) return;

		var formData = new FormData();
		formData.append('contentId', '@Model.Id');

		fetch('/Content/DeleteReview', {
			method: 'POST',
			body: formData
		})
			.then(res => res.json())
			.then(data => {
				if (data.success) {
					location.reload();
				} else {
					alert(data.message);
				}
			});
	}