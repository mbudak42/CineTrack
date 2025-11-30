using Microsoft.EntityFrameworkCore;
using CineTrack.DataAccess.Entities;

namespace CineTrack.DataAccess;

public class CineTrackDbContext : DbContext
{
	public CineTrackDbContext(DbContextOptions<CineTrackDbContext> options)
		: base(options)
	{
	}

	public DbSet<User> Users { get; set; }
	public DbSet<Content> Contents { get; set; }
	public DbSet<Rating> Ratings { get; set; }
	public DbSet<Review> Reviews { get; set; }
	public DbSet<Follow> Follows { get; set; }
	public DbSet<UserList> UserLists { get; set; }
	public DbSet<ListContent> ListContents { get; set; }
	public DbSet<ActivityLog> ActivityLogs { get; set; }
	public DbSet<ActivityLike> ActivityLikes { get; set; }
	public DbSet<ActivityComment> ActivityComments { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(CineTrackDbContext).Assembly);

		// --- ActivityLike Ayarları ---
		modelBuilder.Entity<ActivityLike>()
			.HasKey(al => new { al.UserId, al.ActivityId });

		// Aktivite silinirse -> Beğeni silinsin (Cascade)
		modelBuilder.Entity<ActivityLike>()
			.HasOne(al => al.Activity)
			.WithMany()
			.HasForeignKey(al => al.ActivityId)
			.OnDelete(DeleteBehavior.Cascade);

		// Kullanıcı silinirse -> SQL Hatasını önlemek için NoAction yapıyoruz
		modelBuilder.Entity<ActivityLike>()
			.HasOne(al => al.User)
			.WithMany()
			.HasForeignKey(al => al.UserId)
			.OnDelete(DeleteBehavior.NoAction);


		// --- ActivityComment Ayarları ---

		// Aktivite silinirse -> Yorum silinsin (Cascade)
		modelBuilder.Entity<ActivityComment>()
			.HasOne(ac => ac.Activity)
			.WithMany()
			.HasForeignKey(ac => ac.ActivityId)
			.OnDelete(DeleteBehavior.Cascade);

		// Kullanıcı silinirse -> SQL Hatasını önlemek için NoAction
		modelBuilder.Entity<ActivityComment>()
			.HasOne(ac => ac.User)
			.WithMany()
			.HasForeignKey(ac => ac.UserId)
			.OnDelete(DeleteBehavior.NoAction);
	}

}
