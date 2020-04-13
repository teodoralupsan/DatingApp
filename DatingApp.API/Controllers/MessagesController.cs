using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
    [Route("api/users/{userId}/[controller]")]
    /* Because we added AuthorizeFilter to AuthorizationPolicyBuilder we can remove the [Authorize] attribute
    [Authorize]*/
    [ServiceFilter(typeof(LogUserActivity))]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly IDatingRepository _datingRepository;
        private readonly IMapper _mapper;

        public MessagesController(IDatingRepository datingRepository, IMapper mapper)
        {
            _datingRepository = datingRepository;
            _mapper = mapper;
        }

        [HttpGet("{id}", Name = "GetMessage")]
        public async Task<IActionResult> GetMessage(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            var dbMessage = await _datingRepository.GetMessage(id);
            if (dbMessage == null)
                return NotFound();
            return Ok(dbMessage);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessagesForUser(int userId, [FromQuery]MessageParams messageParams)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            messageParams.UserId = userId;
            var dbMessages = await _datingRepository.GetMessagesForUser(messageParams);
            var messages = _mapper.Map<IEnumerable<MessageToReturnDto>>(dbMessages);
            Response.AddPagination(dbMessages.CurrentPage, dbMessages.PageSize, dbMessages.TotalCount, dbMessages.TotalPages);
            return Ok(messages);
        }

        [HttpGet("thread/{recipientId}")]
        public async Task<IActionResult> GetMessageThread(int userId, int recipientId)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            var dbMessage = await _datingRepository.GetMessageThread(userId, recipientId);
            var messageThread = _mapper.Map<IEnumerable<MessageToReturnDto>>(dbMessage);
            return Ok(messageThread);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId, MessageForCreationDto messageForCreationDto)
        {
            var sender = await _datingRepository.GetUser(userId);
            if (sender.Id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            messageForCreationDto.SenderId = sender.Id;
            var recipient = await _datingRepository.GetUser(messageForCreationDto.RecipientId);
            if (recipient == null)
                return BadRequest("Could not find user");

            var message = _mapper.Map<Message>(messageForCreationDto);
            _datingRepository.Add(message);
            
            if (await _datingRepository.SaveAll())
            {
                var messageToReturn = _mapper.Map<MessageToReturnDto>(message);
                return CreatedAtRoute("GetMessage", new {id = message.Id}, messageToReturn);
            }

            throw new Exception("Creating the message failed on save");
        }

        [HttpPost("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId, int userId)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var dbMessage = await _datingRepository.GetMessage(messageId);
            if (dbMessage.SenderId == userId)
                dbMessage.SenderDeleted = true;

            if (dbMessage.RecipientId == userId)
                dbMessage.RecipientDeleted = true;

            if (dbMessage.SenderDeleted && dbMessage.RecipientDeleted)
                _datingRepository.Delete(dbMessage);

            if (await _datingRepository.SaveAll())
                return NoContent();

            throw new Exception("Error deleting the message");
        }

        [HttpPost("{messageId}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int userId, int messageId)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            var dbMessage = await _datingRepository.GetMessage(messageId);
            if (dbMessage.RecipientId != userId)
                return Unauthorized();

            dbMessage.IsRead = true;
            dbMessage.DateRead = DateTime.Now;

            await _datingRepository.SaveAll();
            return NoContent();
        }
    }
}