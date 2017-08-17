/****** Object:  Table [dbo].[SymbolStats]    Script Date: 2007-10-07 09:40:32 ******/
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[SymbolStats]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[SymbolStats]
GO

/****** Object:  Table [dbo].[ToTrade]    Script Date: 2007-10-07 09:40:32 ******/
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[ToTrade]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[ToTrade]
GO

/****** Object:  Table [dbo].[TradeStats]    Script Date: 2007-10-07 09:40:32 ******/
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[TradeStats]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[TradeStats]
GO

/****** Object:  Table [dbo].[iqF]    Script Date: 2007-10-07 09:40:32 ******/
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[iqF]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[iqF]
GO

/****** Object:  Table [dbo].[iqIndexSymbols]    Script Date: 2007-10-07 09:40:32 ******/
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[iqIndexSymbols]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[iqIndexSymbols]
GO

/****** Object:  Table [dbo].[iqMessageFormats]    Script Date: 2007-10-07 09:40:32 ******/
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[iqMessageFormats]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[iqMessageFormats]
GO

/****** Object:  Table [dbo].[iqRootOptionSymbols]    Script Date: 2007-10-07 09:40:32 ******/
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[iqRootOptionSymbols]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[iqRootOptionSymbols]
GO

/****** Object:  Table [dbo].[iqSymbols]    Script Date: 2007-10-07 09:40:32 ******/
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[iqSymbols]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[iqSymbols]
GO

/****** Object:  Table [dbo].[SymbolStats]    Script Date: 2007-10-07 09:40:33 ******/
CREATE TABLE [dbo].[SymbolStats] (
	[symbol] [varchar] (12) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[hi52wk] [float] NULL ,
	[hi52wkdate] [datetime] NULL ,
	[lo52wk] [float] NULL ,
	[lo52wkdate] [datetime] NULL ,
	[hi26wk] [float] NULL ,
	[hi26wkdate] [datetime] NULL ,
	[lo26wk] [float] NULL ,
	[lo26wkdate] [datetime] NULL ,
	[volatility] [float] NULL ,
	[volume] [float] NULL ,
	[calcdate] [datetime] NULL 
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[ToTrade]    Script Date: 2007-10-07 09:40:33 ******/
CREATE TABLE [dbo].[ToTrade] (
	[symbol] [varchar] (12) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[tradesystem] [varchar] (20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[stop] [float] NULL ,
	[tag1] [float] NULL ,
	[tag2] [float] NULL ,
	[dayhi] [float] NULL ,
	[daylo] [float] NULL ,
	[daycl] [float] NULL ,
	[day3hi] [float] NULL ,
	[day3lo] [float] NULL ,
	[weekhi] [float] NULL ,
	[weeklo] [float] NULL ,
	[weekcl] [float] NULL ,
	[monhi] [float] NULL ,
	[monlo] [float] NULL ,
	[moncl] [float] NULL ,
	[sma20day] [float] NULL ,
	[sma200day] [float] NULL ,
	[sixmonposmean] [float] NULL ,
	[sixmonnegmean] [float] NULL ,
	[sixmonpossd] [float] NULL ,
	[sixmonnegsd] [float] NULL 
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[TradeStats]    Script Date: 2007-10-07 09:40:33 ******/
CREATE TABLE [dbo].[TradeStats] (
	[symbol] [varchar] (12) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[dtentry] [datetime] NOT NULL ,
	[dtexit] [datetime] NULL ,
	[profit] [float] NULL ,
	[side] [varchar] (5) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[bar0open] [float] NOT NULL ,
	[bar0high] [float] NOT NULL ,
	[bar0low] [float] NOT NULL ,
	[bar0close] [float] NOT NULL ,
	[bar0volume] [float] NOT NULL ,
	[bar1open] [float] NULL ,
	[bar1high] [float] NULL ,
	[bar1low] [float] NULL ,
	[bar1close] [float] NULL ,
	[bar1volume] [float] NULL ,
	[sma030] [float] NOT NULL ,
	[sma140] [float] NOT NULL ,
	[sma300] [float] NOT NULL ,
	[sma500] [float] NOT NULL ,
	[sma031] [float] NOT NULL ,
	[sma141] [float] NOT NULL ,
	[sma301] [float] NOT NULL ,
	[sma501] [float] NOT NULL ,
	[sma032] [float] NULL ,
	[sma142] [float] NULL ,
	[sma302] [float] NULL ,
	[sma502] [float] NULL 
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[iqF]    Script Date: 2007-10-07 09:40:34 ******/
CREATE TABLE [dbo].[iqF] (
	[symbol] [varchar] (14) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[exchangeid] [char] (2) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[pe] [float] NULL ,
	[avevol] [float] NULL ,
	[hi52wk] [float] NULL ,
	[lo52wk] [float] NULL ,
	[hicalyr] [float] NULL ,
	[localyr] [float] NULL ,
	[divyield] [float] NULL ,
	[divamt] [float] NULL ,
	[divrate] [float] NULL ,
	[divpaydate] [datetime] NULL ,
	[exdivdate] [datetime] NULL ,
	[epscuryr] [float] NULL ,
	[epsnxtyr] [float] NULL ,
	[grth5yr] [float] NULL ,
	[fsclyrend] [smallint] NULL ,
	[name] [varchar] (100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[pcntinst] [float] NULL ,
	[beta] [float] NULL ,
	[curassets] [float] NULL ,
	[curliable] [float] NULL ,
	[datebalsht] [datetime] NULL ,
	[debtlong] [float] NULL ,
	[shareout] [float] NULL ,
	[split1] [varchar] (15) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[split2] [varchar] (15) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[fmtcode] [smallint] NULL ,
	[numdecdig] [smallint] NULL ,
	[sic] [int] NULL ,
	[histvol] [float] NULL ,
	[sectype] [smallint] NULL ,
	[listmkt] [smallint] NULL ,
	[hi52wkdt] [datetime] NULL ,
	[lo52wkdt] [datetime] NULL ,
	[hicaldate] [datetime] NULL ,
	[localdate] [datetime] NULL 
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[iqIndexSymbols]    Script Date: 2007-10-07 09:40:34 ******/
CREATE TABLE [dbo].[iqIndexSymbols] (
	[symbol] [varchar] (12) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL 
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[iqMessageFormats]    Script Date: 2007-10-07 09:40:34 ******/
CREATE TABLE [dbo].[iqMessageFormats] (
	[MsgType] [char] (1) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[Ord] [smallint] NOT NULL ,
	[FieldName] [varchar] (30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[VarName] [varchar] (10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[FieldType] [varchar] (10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[Note] [varchar] (40) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Width] [smallint] NULL ,
	[viewable] [bit] NOT NULL 
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[iqRootOptionSymbols]    Script Date: 2007-10-07 09:40:34 ******/
CREATE TABLE [dbo].[iqRootOptionSymbols] (
	[symbol] [varchar] (12) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[optionroot] [varchar] (8) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL 
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[iqSymbols]    Script Date: 2007-10-07 09:40:34 ******/
CREATE TABLE [dbo].[iqSymbols] (
	[symbol] [varchar] (14) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[descr] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[exchange] [varchar] (8) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[isindex] [bit] NULL ,
	[iscboeindex] [bit] NULL ,
	[isindicator] [bit] NULL ,
	[ismutualfund] [bit] NULL ,
	[ismoneymarketfund] [bit] NULL 
) ON [PRIMARY]
GO

 CREATE  INDEX [IX_ToTradeTradesystem] ON [dbo].[ToTrade]([tradesystem]) ON [PRIMARY]
GO

 CREATE  INDEX [IX_iqRootOptionSymbols] ON [dbo].[iqRootOptionSymbols]([symbol]) ON [PRIMARY]
GO

