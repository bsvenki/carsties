using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionController : ControllerBase
{
    private readonly IAuctionRepository _repo;

    //private readonly AuctionDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndPoint;

    // Before unit test
    // public AuctionController(AuctionDbContext context, IMapper mapper,IPublishEndpoint publishEndPoint)
     public AuctionController(IAuctionRepository repo, IMapper mapper,IPublishEndpoint publishEndPoint)
    {
        _repo = repo;        
        _mapper = mapper;
       _publishEndPoint = publishEndPoint;
       //_context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
    {
        return await _repo.GetAuctionsAsync(date);
        
        // Moved to AuctionRepository -- Before testing code
        /*
        var query = _context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();

        if(!string.IsNullOrEmpty(date))
        {
            query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
        }
                
        return await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();
        */
    }

    /*
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions()
    {
        var auctions = await _context.Auctions
            .Include(x => x.Item)
            .OrderBy(x => x.Item.Make)
            .ToListAsync();

        return _mapper.Map<List<AuctionDto>>(auctions);
    }
    */

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
    {
        var auction = await _repo.GetAuctionByIdAsync(id);
        /*
        var auction = await _context.Auctions
        .Include(x => x.Item)
        .FirstOrDefaultAsync( x => x.Id == id);
        */

        if(auction == null) return NotFound();

        return auction;

        //return _mapper.Map<AuctionDto>(auction);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
    {
        var auction = _mapper.Map<Auction>(auctionDto);
        
        // TODO: Add current  user as seller
        // auction.Seller = "test";

        auction.Seller = User.Identity.Name;

        // _context.Auctions.Add(auction);
        _repo.AddAuction(auction);

        var newAuction = _mapper.Map<AuctionDto>(auction);

        await _publishEndPoint.Publish(_mapper.Map<AuctionCreated>(newAuction));

        // var result = await _context.SaveChangesAsync() > 0; 
        var result  = await _repo.SaveChangesAsync();   

        if(!result) return BadRequest("Could not save changes to the DB");

        // return CreatedAtAction(nameof(GetAuctionById), new{auction.Id}, _mapper.Map<AuctionDto>(auction));

        return CreatedAtAction(nameof(GetAuctionById), new{auction.Id}, newAuction);


    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuction(Guid id,UpdateAuctionDto updateAuctionDto)
    {
        var auction = await _repo.GetAuctionEntityById(id);
        // var auction =  await _context.Auctions.Include(x => x.Item)
        // .FirstOrDefaultAsync(x => x.Id == id);

        if(auction == null) return NotFound();

        // TODO check seller = username
        if(auction.Seller != User.Identity.Name) return Forbid();

        auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

        await _publishEndPoint.Publish(_mapper.Map<AuctionUpdated>(auction));

        var result = await _repo.SaveChangesAsync();
        // var result = await _context.SaveChangesAsync() > 0;

        if(result) return Ok();

        return BadRequest("problem saving auction changes");

    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
        // var auction = await _context.Auctions.FindAsync(id);
        var auction = await _repo.GetAuctionEntityById(id);

        if(auction == null) return NotFound();

        // TODO check seller = user name
        if(auction.Seller != User.Identity.Name) return Forbid();

        // _context.Auctions.Remove(auction);
        _repo.RemoveAuction(auction);

        await _publishEndPoint.Publish<AuctionDeleted>(new {Id = auction.Id.ToString()});

        // var result = await _context.SaveChangesAsync() >  0;
        var result = await _repo.SaveChangesAsync();

        if(!result) return BadRequest("Could not able to delete auction");

        return Ok();
    }

}