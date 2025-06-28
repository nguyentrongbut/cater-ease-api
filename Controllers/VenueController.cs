using cater_ease_api.Data;
using cater_ease_api.Models;
using cater_ease_api.Dtos.Venue;
using cater_ease_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Slugify;

namespace cater_ease_api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VenueController : ControllerBase
{
    private readonly IMongoCollection<VenueModel> _venues;
    private readonly IMongoCollection<RoomModel> _rooms;
    private readonly CloudinaryService _cloudinary;
    private readonly SlugHelper _slugHelper = new();

    public VenueController(MongoDbService mongoDbService, CloudinaryService cloudinary)
    {
        _venues = mongoDbService.Database.GetCollection<VenueModel>("venues");
        _rooms = mongoDbService.Database.GetCollection<RoomModel>("rooms");
        _cloudinary = cloudinary;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var venues = await _venues.Find(_ => true).ToListAsync();
        return Ok(venues);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var venue = await _venues.Find(v => v.Slug == slug).FirstOrDefaultAsync();
        if (venue == null) return NotFound("Venue not found");

        var rooms = await _rooms.Find(r => venue.RoomIds.Contains(r.Id)).ToListAsync();

        return Ok(new
        {
            venue.Id,
            venue.Name,
            venue.Slug,
            venue.Description,
            venue.Area,
            venue.Price,
            venue.People,
            venue.Table,
            venue.Address,
            venue.Open,
            venue.Close,
            venue.Days,
            venue.HeroBanners,
            venue.ThumbnailImages,
            venue.GalleryImages,
            Rooms = rooms.Select(r => new {
                r.Id,
                r.Name,
                r.Image,
                r.Area,
                r.People,
                r.Table
            })
        });
    }


    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateVenueDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var slug = _slugHelper.GenerateSlug(dto.Name);
        int counter = 1;
        while (await _venues.Find(v => v.Slug == slug).AnyAsync())
        {
            slug = $"{slug}-{counter++}";
        }

        var heroBanners = await UploadImages(dto.HeroBanners);
        var thumbnails = await UploadImages(dto.ThumbnailImages);
        var gallery = await UploadImages(dto.GalleryImages);

        var venue = new VenueModel
        {
            Name = dto.Name,
            Slug = slug,
            Description = dto.Description,
            Area = dto.Area,
            Price = dto.Price,
            People = dto.People,
            Table = dto.Table,
            RoomIds = dto.RoomIds ?? new List<string>(),
            HeroBanners = heroBanners,
            ThumbnailImages = thumbnails,
            GalleryImages = gallery,
            Address = dto.Address,
            Open = dto.Open,
            Close = dto.Close,
            Days = dto.Days ?? new List<string>()
        };

        await _venues.InsertOneAsync(venue);
        return Ok(venue);
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromForm] UpdateVenueDto dto)
    {
        var venue = await _venues.Find(v => v.Id == id).FirstOrDefaultAsync();
        if (venue == null) return NotFound("Venue not found");

        var updates = new List<UpdateDefinition<VenueModel>>();

        if (!string.IsNullOrEmpty(dto.Name))
        {
            updates.Add(Builders<VenueModel>.Update.Set(v => v.Name, dto.Name));
            var slug = _slugHelper.GenerateSlug(dto.Name);
            updates.Add(Builders<VenueModel>.Update.Set(v => v.Slug, slug));
        }

        if (!string.IsNullOrEmpty(dto.Area))
            updates.Add(Builders<VenueModel>.Update.Set(v => v.Area, dto.Area));

        if (dto.Price.HasValue)
            updates.Add(Builders<VenueModel>.Update.Set(v => v.Price, dto.Price.Value));

        if (dto.People.HasValue)
            updates.Add(Builders<VenueModel>.Update.Set(v => v.People, dto.People.Value));

        if (dto.Table.HasValue)
            updates.Add(Builders<VenueModel>.Update.Set(v => v.Table, dto.Table.Value));

        if (!string.IsNullOrEmpty(dto.Description))
            updates.Add(Builders<VenueModel>.Update.Set(v => v.Description, dto.Description));

        if (!string.IsNullOrEmpty(dto.Address))
            updates.Add(Builders<VenueModel>.Update.Set(v => v.Address, dto.Address));

        if (!string.IsNullOrEmpty(dto.Open))
            updates.Add(Builders<VenueModel>.Update.Set(v => v.Open, dto.Open));

        if (!string.IsNullOrEmpty(dto.Close))
            updates.Add(Builders<VenueModel>.Update.Set(v => v.Close, dto.Close));

        if (dto.Days != null)
            updates.Add(Builders<VenueModel>.Update.Set(v => v.Days, dto.Days));

        if (dto.AddRoomIds != null && dto.AddRoomIds.Any())
        {
            venue.RoomIds.AddRange(dto.AddRoomIds.Except(venue.RoomIds));
        }

        if (dto.RemoveRoomIds != null && dto.RemoveRoomIds.Any())
        {
            venue.RoomIds = venue.RoomIds.Except(dto.RemoveRoomIds).ToList();
        }

        updates.Add(Builders<VenueModel>.Update.Set(v => v.RoomIds, venue.RoomIds));

        if (dto.RemoveHeroBanners != null && dto.RemoveHeroBanners.Any())
        {
            foreach (var url in dto.RemoveHeroBanners)
            {
                await _cloudinary.DeleteAsync(url);
            }
            venue.HeroBanners = venue.HeroBanners.Except(dto.RemoveHeroBanners).ToList();
        }

        if (dto.RemoveThumbnailImages != null && dto.RemoveThumbnailImages.Any())
        {
            foreach (var url in dto.RemoveThumbnailImages)
            {
                await _cloudinary.DeleteAsync(url);
            }
            venue.ThumbnailImages = venue.ThumbnailImages.Except(dto.RemoveThumbnailImages).ToList();
        }

        if (dto.RemoveGalleryImages != null && dto.RemoveGalleryImages.Any())
        {
            foreach (var url in dto.RemoveGalleryImages)
            {
                await _cloudinary.DeleteAsync(url);
            }
            venue.GalleryImages = venue.GalleryImages.Except(dto.RemoveGalleryImages).ToList();
        }

        if (dto.AddHeroBanners != null)
        {
            var added = await UploadImages(dto.AddHeroBanners);
            venue.HeroBanners.AddRange(added);
        }

        if (dto.AddThumbnailImages != null)
        {
            var added = await UploadImages(dto.AddThumbnailImages);
            venue.ThumbnailImages.AddRange(added);
        }

        if (dto.AddGalleryImages != null)
        {
            var added = await UploadImages(dto.AddGalleryImages);
            venue.GalleryImages.AddRange(added);
        }

        updates.Add(Builders<VenueModel>.Update.Set(v => v.HeroBanners, venue.HeroBanners.Distinct().ToList()));
        updates.Add(Builders<VenueModel>.Update.Set(v => v.ThumbnailImages, venue.ThumbnailImages.Distinct().ToList()));
        updates.Add(Builders<VenueModel>.Update.Set(v => v.GalleryImages, venue.GalleryImages.Distinct().ToList()));

        if (!updates.Any()) return BadRequest("No data to update.");

        var result = await _venues.UpdateOneAsync(v => v.Id == id, Builders<VenueModel>.Update.Combine(updates));

        return result.ModifiedCount > 0 ? Ok("Venue updated successfully.") : Ok("No changes were made.");
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var venue = await _venues.Find(v => v.Id == id).FirstOrDefaultAsync();
        if (venue == null) return NotFound("Venue not found");

        foreach (var url in venue.HeroBanners.Concat(venue.ThumbnailImages).Concat(venue.GalleryImages).Distinct())
        {
            await _cloudinary.DeleteAsync(url);
        }

        var result = await _venues.DeleteOneAsync(v => v.Id == id);
        return result.DeletedCount == 0 ? NotFound("Delete failed") : Ok("Venue deleted successfully.");
    }

    private async Task<List<string>> UploadImages(List<IFormFile>? files)
    {
        var urls = new List<string>();
        if (files == null || files.Count == 0) return urls;

        foreach (var file in files)
        {
            var url = await _cloudinary.UploadAsync(file);
            urls.Add(url);
        }

        return urls;
    }
}