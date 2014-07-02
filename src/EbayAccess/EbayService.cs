﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EbayAccess.Misc;
using EbayAccess.Models.Credentials;
using EbayAccess.Models.CredentialsAndConfig;
using EbayAccess.Models.GetOrdersResponse;
using EbayAccess.Models.GetSellerListCustomResponse;
using EbayAccess.Models.GetSellerListResponse;
using EbayAccess.Models.ReviseInventoryStatusRequest;
using EbayAccess.Models.ReviseInventoryStatusResponse;
using EbayAccess.Services;
using Item = EbayAccess.Models.GetSellerListCustomResponse.Item;

namespace EbayAccess
{
	public class EbayService : IEbayService
	{
		private const int Maxtimerange = 119;
		private readonly DateTime _ebayWorkingStart = new DateTime( 1995, 1, 1, 0, 0, 0 );

		private IEbayServiceLowLevel EbayServiceLowLevel { get; set; }

		private void PopulateOrdersItemsDetails( IEnumerable< Order > orders )
		{
			foreach( var order in orders )
			{
				foreach( var transaction in order.TransactionArray )
				{
					transaction.Item.ItemDetails = this.EbayServiceLowLevel.GetItem( transaction.Item.ItemId );
					transaction.Item.Sku = transaction.Item.ItemDetails.Sku;
				}
			}
		}

		public EbayService( EbayUserCredentials credentials, EbayConfig ebayConfig, IWebRequestServices webRequestServices )
		{
			this.EbayServiceLowLevel = new EbayServiceLowLevel( credentials, ebayConfig, webRequestServices );
		}

		public EbayService( EbayUserCredentials credentials, EbayConfig ebayConfig ) : this( credentials, ebayConfig, new WebRequestServices() )
		{
		}

		/// <summary>
		/// Just for auth
		/// </summary>
		/// <param name="ebayConfig"></param>
		public EbayService( EbayConfig ebayConfig ) : this( new EbayUserCredentials( "empty", "empty" ), ebayConfig, new WebRequestServices() )
		{
		}

		#region GetOrders
		public IEnumerable< Order > GetOrders( DateTime dateFrom, DateTime dateTo )
		{
			try
			{
				var getOrdersResponse = this.EbayServiceLowLevel.GetOrders( dateFrom, dateTo, GetOrdersTimeRangeEnum.ModTime );

				if( getOrdersResponse.Error != null )
					throw new Exception( string.Format( "Code:{0},ShortMessage:{1},LongMaeesage:{2}", getOrdersResponse.Error.ErrorCode, getOrdersResponse.Error.ShortMessage, getOrdersResponse.Error.LongMessage ) );

				return getOrdersResponse.Orders;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called with({0},{1})", dateFrom, dateTo ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public async Task< IEnumerable< Order > > GetOrdersAsync( DateTime dateFrom, DateTime dateTo )
		{
			try
			{
				var getOrdersResponse = await this.EbayServiceLowLevel.GetOrdersAsync( dateFrom, dateTo, GetOrdersTimeRangeEnum.ModTime ).ConfigureAwait( false );

				if( getOrdersResponse.Error != null )
					throw new Exception( string.Format( "Code:{0},ShortMessage:{1},LongMaeesage:{2}", getOrdersResponse.Error.ErrorCode, getOrdersResponse.Error.ShortMessage, getOrdersResponse.Error.LongMessage ) );

				return getOrdersResponse.Orders;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called with({0},{1})", dateFrom, dateTo ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public async Task< List< string > > GetOrdersIdsAsync( params string[] sourceOrdersIds )
		{
			try
			{
				var existsOrders = new List< string >();

				if( sourceOrdersIds == null || !sourceOrdersIds.Any() )
					return existsOrders;

				var getOrdersResponse = await this.EbayServiceLowLevel.GetOrdersAsync( sourceOrdersIds ).ConfigureAwait( false );

				if( getOrdersResponse.Error != null )
					throw new Exception( string.Format( "Code:{0},ShortMessage:{1},LongMaeesage:{2}", getOrdersResponse.Error.ErrorCode, getOrdersResponse.Error.ShortMessage, getOrdersResponse.Error.LongMessage ) );

				if( getOrdersResponse.Orders == null )
					return existsOrders;

				var distinctOrdersIds = getOrdersResponse.Orders.Distinct( new OrderEqualityComparerById() ).Select( x => x.GetOrderId() ).ToList();

				existsOrders = ( from s in sourceOrdersIds join d in distinctOrdersIds on s equals d select s ).ToList();

				return existsOrders;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called with({0})", string.Join( ",", sourceOrdersIds ) ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		internal class OrderEqualityComparerById : IEqualityComparer< Order >
		{
			public bool Equals( Order x, Order y )
			{
				if( ReferenceEquals( x, y ) )
					return true;

				if( ReferenceEquals( x, null ) || ReferenceEquals( y, null ) )
					return false;

				//Check whether the products' properties are equal. 
				return x.GetOrderId() == y.GetOrderId();
			}

			public int GetHashCode( Order order )
			{
				if( ReferenceEquals( order, null ) )
					return 0;

				var hashProductName = string.IsNullOrWhiteSpace( order.GetOrderId() ) ? 0 : order.GetOrderId().GetHashCode();

				return hashProductName;
			}
		}
		#endregion

		#region GetProducts
		public async Task< IEnumerable< Item > > GetActiveProductsAsync()
		{
			try
			{
				var sellerListsAsync = await this.EbayServiceLowLevel.GetSellerListCustomResponsesAsync( DateTime.UtcNow, DateTime.UtcNow.AddDays( Maxtimerange ), GetSellerListTimeRangeEnum.EndTime ).ConfigureAwait( false );

				if( sellerListsAsync.Any( x => x.Error != null ) )
				{
					var requestsWithErrors = sellerListsAsync.Where( x => x.Error != null ).ToList();
					var requestsWithErrorsInfo = string.Join( "|", requestsWithErrors.Select( x => string.Format( "Code:{0},ShortMessage:{1},LongMaeesage:{2}", x.Error.ErrorCode, x.Error.ShortMessage, x.Error.LongMessage ) ) );
					throw new Exception( requestsWithErrorsInfo );
				}

				var items = sellerListsAsync.SelectMany( x => x.ItemsSplitedByVariations );

				return items;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called with()" ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public async Task< IEnumerable< Item > > GetProductsByEndDateAsync( DateTime endDateFrom, DateTime endDateTo )
		{
			try
			{
				var products = new List< Item >();

				var quartalsStartList = GetListOfTimeRanges( endDateFrom, endDateTo ).ToList();

				var getSellerListAsyncTasks = new List< Task< GetSellerListCustomResponse > >();

				var sellerListAsync = await this.EbayServiceLowLevel.GetSellerListCustomAsync( quartalsStartList[ 0 ], quartalsStartList[ 1 ].AddSeconds( -1 ), GetSellerListTimeRangeEnum.EndTime ).ConfigureAwait( false );

				if( sellerListAsync.Error != null )
				{
					var errorinfo = string.Format( "Code:{0},ShortMessage:{1},LongMaeesage:{2}", sellerListAsync.Error.ErrorCode, sellerListAsync.Error.ShortMessage, sellerListAsync.Error.LongMessage );
					throw new Exception( errorinfo );
				}

				products.AddRange( sellerListAsync.Items );

				for( var i = 1; i < quartalsStartList.Count - 1; i++ )
				{
					getSellerListAsyncTasks.Add( this.EbayServiceLowLevel.GetSellerListCustomAsync( quartalsStartList[ i ], quartalsStartList[ i + 1 ].AddSeconds( -1 ), GetSellerListTimeRangeEnum.EndTime ) );
				}

				await Task.WhenAll( getSellerListAsyncTasks ).ConfigureAwait( false );

				products.AddRange( getSellerListAsyncTasks.SelectMany( task => task.Result.ItemsSplitedByVariations ).ToList() );

				return products;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called with({0},{1})", endDateFrom, endDateTo ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public async Task< IEnumerable< Models.GetSellerListResponse.Item > > GetProductsDetailsAsync( DateTime createTimeFromStart, DateTime createTimeFromTo )
		{
			try
			{
				var products = new List< Models.GetSellerListResponse.Item >();

				var quartalsStartList = GetListOfTimeRanges( createTimeFromStart, createTimeFromTo ).ToList();

				var getSellerListAsyncTasks = new List< Task< GetSellerListResponse > >();

				var sellerListAsync = await this.EbayServiceLowLevel.GetSellerListAsync( quartalsStartList[ 0 ], quartalsStartList[ 1 ].AddSeconds( -1 ), GetSellerListTimeRangeEnum.StartTime ).ConfigureAwait( false );

				if( sellerListAsync.Error != null )
				{
					var errorinfo = string.Format( "Code:{0},ShortMessage:{1},LongMaeesage:{2}", sellerListAsync.Error.ErrorCode, sellerListAsync.Error.ShortMessage, sellerListAsync.Error.LongMessage );
					throw new Exception( errorinfo );
				}

				products.AddRange( sellerListAsync.Items );

				for( var i = 1; i < quartalsStartList.Count - 1; i++ )
				{
					getSellerListAsyncTasks.Add( this.EbayServiceLowLevel.GetSellerListAsync( quartalsStartList[ i ], quartalsStartList[ i + 1 ].AddSeconds( -1 ), GetSellerListTimeRangeEnum.StartTime ) );
				}

				await Task.WhenAll( getSellerListAsyncTasks ).ConfigureAwait( false );

				products.AddRange( getSellerListAsyncTasks.SelectMany( task => task.Result.Items ).ToList() );

				var productsDetails = await this.GetItemsAsync( products ).ConfigureAwait( false );

				var productsDetailsDevidedByVariations = SplitByVariationsOrReturnEmpty( productsDetails );

				return productsDetailsDevidedByVariations;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called with({0},{1})", createTimeFromStart, createTimeFromTo ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public async Task< IEnumerable< Models.GetSellerListResponse.Item > > GetProductsDetailsAsync()
		{
			try
			{
				return await this.GetProductsDetailsAsync( this._ebayWorkingStart, DateTime.Now ).ConfigureAwait( false );
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called with()" ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		protected static IEnumerable< DateTime > GetListOfTimeRanges( DateTime firstQuartalStart, DateTime lastQuartalEnd )
		{
			if( lastQuartalEnd < firstQuartalStart )
				return Enumerable.Empty< DateTime >();

			var quartalsStart = new List< DateTime > { firstQuartalStart };

			while( firstQuartalStart < lastQuartalEnd )
			{
				firstQuartalStart = firstQuartalStart.AddDays( Maxtimerange );
				quartalsStart.Add( firstQuartalStart < lastQuartalEnd ? firstQuartalStart : lastQuartalEnd );
			}

			return quartalsStart;
		}

		protected async Task< IEnumerable< Models.GetSellerListResponse.Item > > GetItemsAsync( IEnumerable< Models.GetSellerListResponse.Item > items )
		{
			var itemsDetailsTasks = items.Select( x => this.EbayServiceLowLevel.GetItemAsync( x.ItemId ) );

			var productsDetails = await Task.WhenAll( itemsDetailsTasks ).ConfigureAwait( false );

			var productsDetailsDevidedByVariations = SplitByVariationsOrReturnEmpty( productsDetails );

			return productsDetailsDevidedByVariations;
		}

		protected static List< Models.GetSellerListResponse.Item > SplitByVariationsOrReturnEmpty( IEnumerable< Models.GetSellerListResponse.Item > productsDetails )
		{
			var productsDetailsDevidedByVariations = new List< Models.GetSellerListResponse.Item >();

			if( productsDetails == null || !productsDetails.Any() )
				return productsDetailsDevidedByVariations;

			foreach( var productDetails in productsDetails )
			{
				if( productDetails.IsItemWithVariations() && productDetails.HaveMultiVariations() )
					productsDetailsDevidedByVariations.AddRange( productDetails.SplitByVariations() );
				else
					productsDetailsDevidedByVariations.Add( productDetails );
			}
			return productsDetailsDevidedByVariations;
		}
		#endregion

		#region UpdateProducts
		public void UpdateProducts( IEnumerable< InventoryStatusRequest > products )
		{
			var commonCallInfo = string.Empty;
			try
			{
				var productsCount = products.Count();
				var productsTemp = products.ToList();
				commonCallInfo = String.Format( "Products count {0} : {1}", productsCount, string.Join( "|", productsTemp.Select( x => string.Format( "Sku:{0},Qty{1}", x.Sku, x.Quantity ) ).ToList() ) );

				var reviseInventoriesStatus = this.EbayServiceLowLevel.ReviseInventoriesStatus( products );

				if( reviseInventoriesStatus.Any( x => x.Error != null ) )
				{
					var requestsWithErrors = reviseInventoriesStatus.Where( x => x.Error != null ).ToList();
					var requestsWithErrorsInfo = string.Join( "|", requestsWithErrors.Select( x => string.Format( "Code:{0},ShortMessage:{1},LongMaeesage:{2}", x.Error.ErrorCode, x.Error.ShortMessage, x.Error.LongMessage ) ) );
					throw new Exception( requestsWithErrorsInfo );
				}
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called with({0})", commonCallInfo ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public async Task< IEnumerable< InventoryStatusResponse > > UpdateProductsAsync( IEnumerable< InventoryStatusRequest > products )
		{
			var commonCallInfo = string.Empty;
			try
			{
				var productsCount = products.Count();
				var productsTemp = products.ToList();
				commonCallInfo = String.Format( "Products count {0} : {1}", productsCount, string.Join( "|", productsTemp.Select( x => string.Format( "Sku:{0},Qty{1}", x.Sku, x.Quantity ) ).ToList() ) );

				var reviseInventoriesStatus = await this.EbayServiceLowLevel.ReviseInventoriesStatusAsync( products ).ConfigureAwait( false );

				if( reviseInventoriesStatus.Any( x => x.Error != null ) )
				{
					var requestsWithErrors = reviseInventoriesStatus.Where( x => x.Error != null ).ToList();
					var requestsWithErrorsInfo = string.Join( "|", requestsWithErrors.Select( x => string.Format( "Code:{0},ShortMessage:{1},LongMaeesage:{2}", x.Error.ErrorCode, x.Error.ShortMessage, x.Error.LongMessage ) ) );
					throw new Exception( requestsWithErrorsInfo );
				}

				return reviseInventoriesStatus;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayCommonException( string.Format( "Error. Was called with({0})", commonCallInfo ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}
		#endregion

		#region Authentication
		public string GetUserToken()
		{
			try
			{
				var sessionId = this.EbayServiceLowLevel.GetSessionId();
				this.EbayServiceLowLevel.AuthenticateUser( sessionId );
				var userToken = this.EbayServiceLowLevel.FetchToken( sessionId );
				return userToken;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayAuthException( string.Format( "Error. Was called with()" ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public string GetUserSessionId()
		{
			try
			{
				var sessionId = this.EbayServiceLowLevel.GetSessionId();
				return sessionId;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayAuthException( string.Format( "Error. Was called with()" ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public string GetAuthUri( string sessionId )
		{
			try
			{
				var uri = this.EbayServiceLowLevel.GetAuthenticationUri( sessionId );
				return uri.AbsoluteUri;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayAuthException( string.Format( "Error. Was called with({0})", sessionId ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}

		public string FetchUserToken( string sessionId )
		{
			try
			{
				var userToken = this.EbayServiceLowLevel.FetchToken( sessionId );
				return userToken;
			}
			catch( Exception exception )
			{
				var ebayException = new EbayAuthException( string.Format( "Error. Was called with({0})", sessionId ), exception );
				LogTraceException( ebayException.Message, ebayException );
				throw ebayException;
			}
		}
		#endregion

		private static void LogTraceException( string message, EbayException ebayException )
		{
			EbayLogger.Log().Trace( message, ebayException );
		}
	}
}