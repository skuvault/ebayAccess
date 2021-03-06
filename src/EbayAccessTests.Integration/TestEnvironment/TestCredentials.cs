﻿using System.Linq;
using EbayAccess;
using EbayAccess.Models.Credentials;
using EbayAccess.Models.CredentialsAndConfig;
using LINQtoCSV;

namespace EbayAccessTests.Integration.TestEnvironment
{
	public class TestCredentials
	{
		private readonly FlatUserCredentialsCsvLine _flatUserCredentialsCsvLine;
		private readonly FlatDevCredentialCsvLine _flatDevCredentialCsvLine;
		private readonly FlatRuNameCsvLine _ruNameCsvLines;

		public TestCredentials( string credentialsFilePath, string devCredentialsFilePath, string saleItemsIdsFilePath, string runameFilePath )
		{
			var cc = new CsvContext();
			this._flatUserCredentialsCsvLine = Enumerable.FirstOrDefault( cc.Read< FlatUserCredentialsCsvLine >( credentialsFilePath, new CsvFileDescription { FirstLineHasColumnNames = true } ) );
			this._flatDevCredentialCsvLine = Enumerable.FirstOrDefault( cc.Read< FlatDevCredentialCsvLine >( devCredentialsFilePath, new CsvFileDescription { FirstLineHasColumnNames = true } ) );
			this._ruNameCsvLines = Enumerable.FirstOrDefault( cc.Read< FlatRuNameCsvLine >( runameFilePath, new CsvFileDescription { FirstLineHasColumnNames = true } ) );
		}

		public EbayUserCredentials GetEbayUserCredentials()
		{
			return new EbayUserCredentials( this._flatUserCredentialsCsvLine.AccountName, this._flatUserCredentialsCsvLine.Token, ( int )ebaySites.AU );
		}

		public EbayConfig GetEbayConfigSandbox()
		{
			return new EbayConfigSandBox( this._flatDevCredentialCsvLine.AppName, this._flatDevCredentialCsvLine.DevName, this._flatDevCredentialCsvLine.CertName, this._ruNameCsvLines.RuName );
		}

		public EbayConfig GetEbayConfigProduction()
		{
			return new EbayConfig( this._flatDevCredentialCsvLine.AppName, this._flatDevCredentialCsvLine.DevName, this._flatDevCredentialCsvLine.CertName, this._ruNameCsvLines.RuName );
		}

		public string GetRuName()
		{
			return this._ruNameCsvLines.RuName;
		}

		internal class FlatUserCredentialsCsvLine
		{
			public FlatUserCredentialsCsvLine()
			{
			}

			[ CsvColumn( Name = "AccountName", FieldIndex = 1 ) ]
			public string AccountName { get; set; }

			[ CsvColumn( Name = "Token", FieldIndex = 2 ) ]
			public string Token { get; set; }
		}

		internal class FlatDevCredentialCsvLine
		{
			public FlatDevCredentialCsvLine()
			{
			}

			[ CsvColumn( Name = "AppName", FieldIndex = 1 ) ]
			public string AppName { get; set; }

			[ CsvColumn( Name = "DevName", FieldIndex = 2 ) ]
			public string DevName { get; set; }

			[ CsvColumn( Name = "CertName", FieldIndex = 3 ) ]
			public string CertName { get; set; }
		}

		internal class FlatRuNameCsvLine
		{
			public FlatRuNameCsvLine()
			{
			}

			[ CsvColumn( Name = "RuName", FieldIndex = 1 ) ]
			public string RuName { get; set; }
		}
	}
}