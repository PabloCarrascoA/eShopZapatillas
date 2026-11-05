using System;
namespace Catalog.API.Integration.ItemEvents;

public class ItemPriceChangedEvent
{
	public ItemPriceChangedEvent
	{
		public string Name { get; set; }
		public decimal Price { get; set; }
}
}
