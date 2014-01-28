﻿using System;
using System.Threading.Tasks;
using Netco.ActionPolicyServices;
using Netco.Logging;
using Netco.Utils;

namespace EbayAccess.Misc
{
	public static class ActionPolicies
	{
		public static ActionPolicy Submit
		{
			get { return _ebaySumbitPolicy; }
		}

		private static readonly ActionPolicy _ebaySumbitPolicy = ActionPolicy.Handle<Exception>().Retry(10, (ex, i) =>
		{
			typeof(ActionPolicies).Log().Trace(ex, "Retrying Ebay API submit call for the {0} time", i);
			SystemUtil.Sleep(TimeSpan.FromSeconds(0.5 + i));
		});

		public static ActionPolicyAsync SubmitAsync
		{
			get { return _ebaySumbitAsyncPolicy; }
		}

		private static readonly ActionPolicyAsync _ebaySumbitAsyncPolicy = ActionPolicyAsync.Handle<Exception>().RetryAsync(10, async (ex, i) =>
		{
			typeof(ActionPolicies).Log().Trace(ex, "Retrying Ebay API submit call for the {0} time", i);
			await Task.Delay(TimeSpan.FromSeconds(0.5 + i));
		});

		public static ActionPolicy Get
		{
			get { return _ebayGetPolicy; }
		}

		private static readonly ActionPolicy _ebayGetPolicy = ActionPolicy.Handle<Exception>().Retry(10, (ex, i) =>
		{
			typeof(ActionPolicies).Log().Trace(ex, "Retrying Ebay API get call for the {0} time", i);
			SystemUtil.Sleep(TimeSpan.FromSeconds(0.5 + i));
		});

		public static ActionPolicyAsync GetAsync
		{
			get { return _ebayGetAsyncPolicy; }
		}

		private static readonly ActionPolicyAsync _ebayGetAsyncPolicy = ActionPolicyAsync.Handle<Exception>().RetryAsync(10, async (ex, i) =>
		{
			typeof(ActionPolicies).Log().Trace(ex, "Retrying Ebay API get call for the {0} time", i);
			await Task.Delay(TimeSpan.FromSeconds(0.5 + i));
		});
	}
}