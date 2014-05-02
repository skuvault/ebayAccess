﻿using System.IO;
using EbayAccess.Services.Parsers;
using FluentAssertions;
using NUnit.Framework;

namespace EbayAccessTests.Services.Parsers
{
	[ TestFixture ]
	public class EbayGetSallerListResponseParserTest
	{
		[ Test ]
		public void FileStreamWithCorrectXml_ParseItemsResponse_HookupCorrectDeserializedObject()
		{
			//A
			using(
				var fs = new FileStream( @".\Files\EbayServiceGetSellerListResponseWith3Items_DetailLevelAll.xml", FileMode.Open,
					FileAccess.Read ) )
			{
				//A
				var orders = new EbayGetSallerListResponseParser().Parse( fs );

				//A
				orders.Items.Should().HaveCount( 3, "because in source file there is {0} items", 3 );
			}
		}
	}
}