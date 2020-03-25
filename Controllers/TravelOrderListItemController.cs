﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelManagementApi.Models;
using TravelManagementApi.Models.TravelOrderDocument;
using TravelManagementApi.Models.TravelOrderList;

namespace TravelManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TravelOrderListItemController : ControllerBase
    {
        IWebHostEnvironment _webHostingEnvironment;
        private readonly TravelOrderListContext _travelOrderListcontext;
        private readonly TravelOrderDocumentContext _travelOrderDocumentContext;

        public TravelOrderListItemController(IWebHostEnvironment webHostingEnvironment, TravelOrderListContext travelOrderListContext, TravelOrderDocumentContext travelOrderDocumentContext)
        {
            _webHostingEnvironment = webHostingEnvironment;
            _travelOrderListcontext = travelOrderListContext;
            _travelOrderDocumentContext = travelOrderDocumentContext;
        }

        // GET: api/TravelOrderListItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TravelOrderListItem>>> GetTravelOrderListItems()
        {
            return await _travelOrderListcontext.TravelOrderListItems.ToListAsync();
        }

        // GET: api/TravelOrderListItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TravelOrderListItem>> GetTravelOrderListItem(long id)
        {
            var travelOrderItem = await _travelOrderListcontext.TravelOrderListItems.FindAsync(id);

            if (travelOrderItem == null)
            {
                return NotFound();
            }

            return travelOrderItem;
        }

        // PUT: api/TravelOrderListItems/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTravelOrderListItem(long id, TravelOrderListItem travelOrderItem)
        {
            if (id != travelOrderItem.Id)
            {
                return BadRequest();
            }

            _travelOrderListcontext.Entry(travelOrderItem).State = EntityState.Modified;

            try
            {
                await _travelOrderListcontext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TravelOrderItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/TravelOrderListItems
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<TravelOrderListItem>> PostTravelOrderListItem([FromForm(Name = "document")] IFormFile travelOrderListItemFormFile)
        {
            //Excel part
            var travelOrderListItemManager = new TravelOrderListItemManager(travelOrderListItemFormFile);

            var uploads = Path.Combine(_webHostingEnvironment.ContentRootPath, "uploads/spreadsheets");
            var filePath = Path.Combine(uploads, travelOrderListItemManager.ListName);

            var travelOrderListItem = await travelOrderListItemManager.SaveListAsync(filePath);

            _travelOrderListcontext.TravelOrderListItems.Add(travelOrderListItem);
            await _travelOrderListcontext.SaveChangesAsync();

            var travelOrderDataItems = travelOrderListItemManager.GetExtractedListData();

            // Word Part
            var documentTemplatePath = Path.Combine(_webHostingEnvironment.ContentRootPath, "uploads/templates/DocumentTemplate.docx");
            var generatedDocumentsPath = Path.Combine(_webHostingEnvironment.ContentRootPath, "uploads/documents");

            var travelOrderDocumentManager = new TravelOrderDocumentManager(documentTemplatePath, travelOrderDataItems, travelOrderListItem.Id, generatedDocumentsPath);

            var travelOrderDocumentItems = travelOrderDocumentManager.GenerateDocumentsFromData();


            _travelOrderDocumentContext.AddRange(travelOrderDocumentItems);
            await _travelOrderDocumentContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTravelOrderListItem), new { id = travelOrderListItem.Id }, travelOrderListItem);

        }

        // DELETE: api/TravelOrderListItems/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<TravelOrderListItem>> DeleteTravelOrderListItem(long id)
        {
            var travelOrderItem = await _travelOrderListcontext.TravelOrderListItems.FindAsync(id);
            if (travelOrderItem == null)
            {
                return NotFound();
            }

            _travelOrderListcontext.TravelOrderListItems.Remove(travelOrderItem);
            await _travelOrderListcontext.SaveChangesAsync();

            return travelOrderItem;
        }

        private bool TravelOrderItemExists(long id)
        {
            return _travelOrderListcontext.TravelOrderListItems.Any(e => e.Id == id);
        }
    }
}
