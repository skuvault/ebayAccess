﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;
using EbayAccess.Models.BaseResponse;

namespace EbayAccess.Services.Parsers
{
	public class EbayPagesParser
	{
		public PaginationResult ParsePaginationResultResponse( Stream stream, bool keepStremPosition = true )
		{
			try
			{
				//
				//var streamReader = new StreamReader( stream );
				//string tempstr = streamReader.ReadToEnd();
				//stream.Position = 0;
				//

				PaginationResult res = null;

				XNamespace ns = "urn:ebay:apis:eBLBaseComponents";

				var root = XElement.Load( stream );

				object temp = null;

				var paginationResultElement = root.Element( ns + "PaginationResult" );
				if( paginationResultElement != null )
				{
					res = new PaginationResult();

					if( this.GetElementValue( paginationResultElement, ref temp, ns, "TotalNumberOfPages" ) )
						res.TotalNumberOfPages = int.Parse( ( string )temp );

					if( this.GetElementValue( paginationResultElement, ref temp, ns, "TotalNumberOfEntries" ) )
						res.TotalNumberOfEntries = int.Parse( ( string )temp );
				}

				if( keepStremPosition )
					stream.Position = 0;

				return res;
			}
			catch( Exception ex )
			{
				var buffer = new byte[ stream.Length ];
				stream.Read( buffer, 0, ( int )stream.Length );
				var utf8Encoding = new UTF8Encoding();
				var bufferStr = utf8Encoding.GetString( buffer );
				throw new Exception( "Can't parse: " + bufferStr, ex );
			}
		}

		private bool GetElementValue( XElement x, ref object parsedElement, XNamespace ns, params string[] elementName )
		{
			if( elementName.Length > 0 )
			{
				var element = x.Element( ns + elementName[ 0 ] );
				if( element != null )
				{
					if( elementName.Length > 1 )
						return this.GetElementValue( element, ref parsedElement, ns, elementName.Skip( 1 ).ToArray() );
					parsedElement = element.Value;
					return true;
				}
			}

			return false;
		}

		private object GetElementValue( XElement x, XNamespace ns, params string[] elementName )
		{
			object parsedElement = null;
			this.GetElementValue( x, ref parsedElement, ns, elementName );
			return parsedElement;
		}

		public PaginationResult ParsePaginationResultResponse( WebResponse response )
		{
			PaginationResult result = null;
			using( var responseStream = response.GetResponseStream() )
			{
				if( responseStream != null )
				{
					using( var memStream = new MemoryStream() )
					{
						responseStream.CopyTo( memStream, 0x100 );
						result = this.ParsePaginationResultResponse( memStream );
					}
				}
			}

			return result;
		}
	}
}