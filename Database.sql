USE [master]
GO
/****** Object:  Database [CustomerAndServerMaintenanceTracking]    Script Date: 6/20/2025 9:42:12 pm ******/
CREATE DATABASE [CustomerAndServerMaintenanceTracking]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'CustomerAndServerMaintenanceTracking', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\CustomerAndServerMaintenanceTracking.mdf' , SIZE = 20954688KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'CustomerAndServerMaintenanceTracking_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\CustomerAndServerMaintenanceTracking_log.ldf' , SIZE = 6234112KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET COMPATIBILITY_LEVEL = 150
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [CustomerAndServerMaintenanceTracking].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET ARITHABORT OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET  DISABLE_BROKER 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET  MULTI_USER 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET DB_CHAINING OFF 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'CustomerAndServerMaintenanceTracking', N'ON'
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET QUERY_STORE = OFF
GO
USE [CustomerAndServerMaintenanceTracking]
GO
/****** Object:  User [server]    Script Date: 6/20/2025 9:42:12 pm ******/
CREATE USER [server] FOR LOGIN [server] WITH DEFAULT_SCHEMA=[dbo]
GO
ALTER ROLE [db_owner] ADD MEMBER [server]
GO
ALTER ROLE [db_accessadmin] ADD MEMBER [server]
GO
ALTER ROLE [db_datareader] ADD MEMBER [server]
GO
ALTER ROLE [db_datawriter] ADD MEMBER [server]
GO
/****** Object:  Table [dbo].[ApplicationLogs]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ApplicationLogs](
	[LogId] [int] IDENTITY(1,1) NOT NULL,
	[LogTimestamp] [datetime] NOT NULL,
	[ServiceName] [nvarchar](100) NOT NULL,
	[LogLevel] [nvarchar](20) NOT NULL,
	[Message] [nvarchar](max) NOT NULL,
	[ExceptionDetails] [nvarchar](max) NULL,
 CONSTRAINT [PK_ApplicationLogs] PRIMARY KEY CLUSTERED 
(
	[LogId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Barangays]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Barangays](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MunicipalityId] [int] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[DateAdded] [datetime] NOT NULL,
 CONSTRAINT [PK_Barangays] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Barangays_MunicipalityId_Name] UNIQUE NONCLUSTERED 
(
	[MunicipalityId] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ClusterTagMapping]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ClusterTagMapping](
	[ClusterId] [int] NOT NULL,
	[TagId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ClusterId] ASC,
	[TagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Customers]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Customers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AccountName] [nvarchar](100) NOT NULL,
	[AdditionalName] [nvarchar](100) NULL,
	[ContactNumber] [nvarchar](50) NULL,
	[Email] [nvarchar](100) NULL,
	[Location] [nvarchar](100) NULL,
	[IsArchived] [bit] NOT NULL,
	[IPAddress] [nvarchar](50) NULL,
	[RouterId] [int] NULL,
	[MacAddress] [nvarchar](17) NULL,
	[MikrotikSecretId] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CustomerTags]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CustomerTags](
	[CustomerId] [int] NOT NULL,
	[TagId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CustomerId] ASC,
	[TagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceIPs]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceIPs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DeviceName] [varchar](100) NOT NULL,
	[IPAddress] [varchar](50) NULL,
	[Location] [varchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceIPTags]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceIPTags](
	[DeviceIPId] [int] NOT NULL,
	[TagId] [int] NOT NULL,
 CONSTRAINT [PK_DeviceIPTags] PRIMARY KEY CLUSTERED 
(
	[DeviceIPId] ASC,
	[TagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EmailSettings]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EmailSettings](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SettingName] [nvarchar](100) NOT NULL,
	[SmtpServer] [nvarchar](255) NOT NULL,
	[SmtpPort] [int] NOT NULL,
	[EnableSsl] [bit] NOT NULL,
	[SenderEmail] [nvarchar](255) NOT NULL,
	[SenderDisplayName] [nvarchar](255) NULL,
	[SmtpUsername] [nvarchar](255) NULL,
	[SmtpPassword] [nvarchar](max) NULL,
	[IsDefault] [bit] NOT NULL,
 CONSTRAINT [PK_EmailSettings] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_EmailSettings_SettingName] UNIQUE NONCLUSTERED 
(
	[SettingName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Municipalities]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Municipalities](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[DateAdded] [datetime] NOT NULL,
 CONSTRAINT [PK_Municipalities] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Municipalities_Name] UNIQUE NONCLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NetwatchConfigs]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NetwatchConfigs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[NetwatchName] [nvarchar](255) NOT NULL,
	[Type] [varchar](50) NOT NULL,
	[IntervalSeconds] [int] NOT NULL,
	[TimeoutMilliseconds] [int] NOT NULL,
	[SourceType] [varchar](50) NOT NULL,
	[TargetId] [int] NOT NULL,
	[IsEnabled] [bit] NOT NULL,
	[RunUponSave] [bit] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[LastChecked] [datetime] NULL,
	[LastStatus] [nvarchar](255) NULL,
 CONSTRAINT [PK_NetwatchConfigs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NetwatchConfigTags]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NetwatchConfigTags](
	[NetwatchConfigId] [int] NOT NULL,
	[TagId] [int] NOT NULL,
 CONSTRAINT [PK_NetwatchConfigTags] PRIMARY KEY CLUSTERED 
(
	[NetwatchConfigId] ASC,
	[TagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NetwatchIpResults]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NetwatchIpResults](
	[NetwatchIpResultId] [int] IDENTITY(1,1) NOT NULL,
	[NetwatchConfigId] [int] NOT NULL,
	[IpAddress] [nvarchar](50) NOT NULL,
	[EntityName] [nvarchar](100) NULL,
	[LastPingStatus] [nvarchar](50) NOT NULL,
	[RoundtripTimeMs] [bigint] NULL,
	[LastPingAttemptDateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_NetwatchIpResults] PRIMARY KEY CLUSTERED 
(
	[NetwatchIpResultId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NetwatchOutageLog]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NetwatchOutageLog](
	[OutageLogId] [int] IDENTITY(1,1) NOT NULL,
	[NetwatchConfigId] [int] NOT NULL,
	[IpAddress] [varchar](50) NOT NULL,
	[EntityName] [nvarchar](255) NULL,
	[OutageStartTime] [datetime] NOT NULL,
	[OutageEndTime] [datetime] NULL,
	[LastPingStatusAtStart] [nvarchar](50) NULL,
 CONSTRAINT [PK_NetwatchOutageLog] PRIMARY KEY CLUSTERED 
(
	[OutageLogId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NetworkClusters]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NetworkClusters](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClusterName] [nvarchar](255) NOT NULL,
	[ClusterDescription] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NotificationHistoryLogs]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NotificationHistoryLogs](
	[HistoryLogId] [int] IDENTITY(1,1) NOT NULL,
	[RuleId] [int] NOT NULL,
	[LogTimestamp] [datetime] NOT NULL,
	[LogLevel] [nvarchar](50) NOT NULL,
	[Message] [nvarchar](max) NOT NULL,
	[ExceptionDetails] [nvarchar](max) NULL,
 CONSTRAINT [PK_NotificationHistoryLogs] PRIMARY KEY CLUSTERED 
(
	[HistoryLogId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NotificationRules]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NotificationRules](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RuleName] [nvarchar](255) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[NotificationChannelsJson] [nvarchar](max) NULL,
	[SourceFeature] [nvarchar](100) NULL,
	[SourceEntityId] [int] NULL,
	[TriggerDetailsJson] [nvarchar](max) NULL,
	[ContentDetailsJson] [nvarchar](max) NULL,
	[RecipientDetailsJson] [nvarchar](max) NULL,
	[ScheduleDetailsJson] [nvarchar](max) NULL,
	[IsEnabled] [bit] NOT NULL,
	[DateCreated] [datetime] NOT NULL,
	[LastModified] [datetime] NULL,
	[LastRunTime] [datetime] NULL,
	[NextRunTime] [datetime] NULL,
	[RunCount] [int] NOT NULL,
 CONSTRAINT [PK_NotificationRules] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RolePermissions]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RolePermissions](
	[RoleId] [int] NOT NULL,
	[PermissionKey] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED 
(
	[RoleId] ASC,
	[PermissionKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Routers]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Routers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RouterName] [nvarchar](100) NOT NULL,
	[HostIPAddress] [nvarchar](50) NOT NULL,
	[Username] [nvarchar](50) NOT NULL,
	[Password] [nvarchar](100) NOT NULL,
	[ApiPort] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ServiceHeartbeats]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ServiceHeartbeats](
	[ServiceName] [varchar](100) NOT NULL,
	[LastHeartbeatDateTime] [datetime] NOT NULL,
	[ServiceStatus] [varchar](50) NULL,
	[LastDataSyncTimestamp] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[ServiceName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TagAssignments]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TagAssignments](
	[ParentTagId] [int] NOT NULL,
	[ChildTagId] [int] NOT NULL,
 CONSTRAINT [PK_TagAssignments] PRIMARY KEY CLUSTERED 
(
	[ParentTagId] ASC,
	[ChildTagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tags]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tags](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TagName] [nvarchar](100) NOT NULL,
	[TagDescription] [nvarchar](255) NULL,
	[IsParent] [bit] NOT NULL,
	[TagType] [varchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserAccounts]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserAccounts](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FullName] [nvarchar](150) NULL,
	[Username] [nvarchar](50) NOT NULL,
	[PasswordHash] [nvarchar](max) NOT NULL,
	[Email] [nvarchar](100) NULL,
	[PhoneNumber] [nvarchar](50) NULL,
	[RoleId] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[DateCreated] [datetime] NOT NULL,
	[LastLoginDate] [datetime] NULL,
	[DeactivationReason] [nvarchar](max) NULL,
 CONSTRAINT [PK_UserAccounts] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_UserAccounts_Username] UNIQUE NONCLUSTERED 
(
	[Username] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserRoles]    Script Date: 6/20/2025 9:42:12 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRoles](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RoleName] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](255) NULL,
	[DateCreated] [datetime] NOT NULL,
 CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_UserRoles_RoleName] UNIQUE NONCLUSTERED 
(
	[RoleName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_ApplicationLogs_Timestamp_Service]    Script Date: 6/20/2025 9:42:12 pm ******/
CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_Timestamp_Service] ON [dbo].[ApplicationLogs]
(
	[LogTimestamp] DESC,
	[ServiceName] ASC
)
INCLUDE([LogLevel],[Message]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Customers_MikrotikSecretId_RouterId]    Script Date: 6/20/2025 9:42:12 pm ******/
CREATE NONCLUSTERED INDEX [IX_Customers_MikrotikSecretId_RouterId] ON [dbo].[Customers]
(
	[RouterId] ASC,
	[MikrotikSecretId] ASC
)
INCLUDE([AccountName]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Customers_RouterId]    Script Date: 6/20/2025 9:42:12 pm ******/
CREATE NONCLUSTERED INDEX [IX_Customers_RouterId] ON [dbo].[Customers]
(
	[RouterId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_NetwatchIpResults_NetwatchConfigId]    Script Date: 6/20/2025 9:42:12 pm ******/
CREATE NONCLUSTERED INDEX [IX_NetwatchIpResults_NetwatchConfigId] ON [dbo].[NetwatchIpResults]
(
	[NetwatchConfigId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_NetwatchOutageLog_IpAddress_Ongoing]    Script Date: 6/20/2025 9:42:12 pm ******/
CREATE NONCLUSTERED INDEX [IX_NetwatchOutageLog_IpAddress_Ongoing] ON [dbo].[NetwatchOutageLog]
(
	[NetwatchConfigId] ASC,
	[IpAddress] ASC,
	[OutageEndTime] ASC
)
INCLUDE([OutageStartTime]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_NetwatchOutageLog_OutageStartTime]    Script Date: 6/20/2025 9:42:12 pm ******/
CREATE NONCLUSTERED INDEX [IX_NetwatchOutageLog_OutageStartTime] ON [dbo].[NetwatchOutageLog]
(
	[OutageStartTime] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_NotificationHistoryLogs_RuleId]    Script Date: 6/20/2025 9:42:12 pm ******/
CREATE NONCLUSTERED INDEX [IX_NotificationHistoryLogs_RuleId] ON [dbo].[NotificationHistoryLogs]
(
	[RuleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_NotificationRules_NextRunTime_IsEnabled]    Script Date: 6/20/2025 9:42:12 pm ******/
CREATE NONCLUSTERED INDEX [IX_NotificationRules_NextRunTime_IsEnabled] ON [dbo].[NotificationRules]
(
	[IsEnabled] ASC,
	[NextRunTime] ASC
)
INCLUDE([Id]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ApplicationLogs] ADD  CONSTRAINT [DF_ApplicationLogs_LogTimestamp]  DEFAULT (getdate()) FOR [LogTimestamp]
GO
ALTER TABLE [dbo].[Barangays] ADD  DEFAULT (getdate()) FOR [DateAdded]
GO
ALTER TABLE [dbo].[Customers] ADD  DEFAULT ((0)) FOR [IsArchived]
GO
ALTER TABLE [dbo].[Municipalities] ADD  DEFAULT (getdate()) FOR [DateAdded]
GO
ALTER TABLE [dbo].[NetwatchConfigs] ADD  CONSTRAINT [DF_NetwatchConfigs_Type]  DEFAULT ('ICMP') FOR [Type]
GO
ALTER TABLE [dbo].[NetwatchConfigs] ADD  CONSTRAINT [DF_NetwatchConfigs_IsEnabled]  DEFAULT ((1)) FOR [IsEnabled]
GO
ALTER TABLE [dbo].[NetwatchConfigs] ADD  CONSTRAINT [DF_NetwatchConfigs_RunUponSave]  DEFAULT ((0)) FOR [RunUponSave]
GO
ALTER TABLE [dbo].[NetwatchConfigs] ADD  CONSTRAINT [DF_NetwatchConfigs_CreatedDate]  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[NotificationRules] ADD  CONSTRAINT [DF_NotificationRules_IsEnabled]  DEFAULT ((1)) FOR [IsEnabled]
GO
ALTER TABLE [dbo].[NotificationRules] ADD  CONSTRAINT [DF_NotificationRules_DateCreated]  DEFAULT (getdate()) FOR [DateCreated]
GO
ALTER TABLE [dbo].[NotificationRules] ADD  CONSTRAINT [DF_NotificationRules_RunCount]  DEFAULT ((0)) FOR [RunCount]
GO
ALTER TABLE [dbo].[Routers] ADD  CONSTRAINT [DF_Routers_ApiPort]  DEFAULT ((8728)) FOR [ApiPort]
GO
ALTER TABLE [dbo].[Tags] ADD  DEFAULT ((0)) FOR [IsParent]
GO
ALTER TABLE [dbo].[Tags] ADD  DEFAULT ('Customer') FOR [TagType]
GO
ALTER TABLE [dbo].[UserAccounts] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[UserAccounts] ADD  DEFAULT (getdate()) FOR [DateCreated]
GO
ALTER TABLE [dbo].[UserRoles] ADD  DEFAULT (getdate()) FOR [DateCreated]
GO
ALTER TABLE [dbo].[Barangays]  WITH CHECK ADD  CONSTRAINT [FK_Barangays_Municipalities] FOREIGN KEY([MunicipalityId])
REFERENCES [dbo].[Municipalities] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Barangays] CHECK CONSTRAINT [FK_Barangays_Municipalities]
GO
ALTER TABLE [dbo].[ClusterTagMapping]  WITH CHECK ADD FOREIGN KEY([ClusterId])
REFERENCES [dbo].[NetworkClusters] ([Id])
GO
ALTER TABLE [dbo].[ClusterTagMapping]  WITH CHECK ADD FOREIGN KEY([TagId])
REFERENCES [dbo].[Tags] ([Id])
GO
ALTER TABLE [dbo].[Customers]  WITH CHECK ADD  CONSTRAINT [FK_Customers_Routers] FOREIGN KEY([RouterId])
REFERENCES [dbo].[Routers] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[Customers] CHECK CONSTRAINT [FK_Customers_Routers]
GO
ALTER TABLE [dbo].[CustomerTags]  WITH CHECK ADD FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Customers] ([Id])
GO
ALTER TABLE [dbo].[CustomerTags]  WITH CHECK ADD FOREIGN KEY([TagId])
REFERENCES [dbo].[Tags] ([Id])
GO
ALTER TABLE [dbo].[DeviceIPTags]  WITH CHECK ADD  CONSTRAINT [FK_DeviceIPTags_DeviceIPs] FOREIGN KEY([DeviceIPId])
REFERENCES [dbo].[DeviceIPs] ([Id])
GO
ALTER TABLE [dbo].[DeviceIPTags] CHECK CONSTRAINT [FK_DeviceIPTags_DeviceIPs]
GO
ALTER TABLE [dbo].[DeviceIPTags]  WITH CHECK ADD  CONSTRAINT [FK_DeviceIPTags_Tags] FOREIGN KEY([TagId])
REFERENCES [dbo].[Tags] ([Id])
GO
ALTER TABLE [dbo].[DeviceIPTags] CHECK CONSTRAINT [FK_DeviceIPTags_Tags]
GO
ALTER TABLE [dbo].[NetwatchConfigTags]  WITH CHECK ADD  CONSTRAINT [FK_NetwatchConfigTags_NetwatchConfigs] FOREIGN KEY([NetwatchConfigId])
REFERENCES [dbo].[NetwatchConfigs] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[NetwatchConfigTags] CHECK CONSTRAINT [FK_NetwatchConfigTags_NetwatchConfigs]
GO
ALTER TABLE [dbo].[NetwatchConfigTags]  WITH CHECK ADD  CONSTRAINT [FK_NetwatchConfigTags_Tags] FOREIGN KEY([TagId])
REFERENCES [dbo].[Tags] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[NetwatchConfigTags] CHECK CONSTRAINT [FK_NetwatchConfigTags_Tags]
GO
ALTER TABLE [dbo].[NetwatchIpResults]  WITH CHECK ADD  CONSTRAINT [FK_NetwatchIpResults_NetwatchConfigs] FOREIGN KEY([NetwatchConfigId])
REFERENCES [dbo].[NetwatchConfigs] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[NetwatchIpResults] CHECK CONSTRAINT [FK_NetwatchIpResults_NetwatchConfigs]
GO
ALTER TABLE [dbo].[NetwatchOutageLog]  WITH CHECK ADD  CONSTRAINT [FK_NetwatchOutageLog_NetwatchConfigs] FOREIGN KEY([NetwatchConfigId])
REFERENCES [dbo].[NetwatchConfigs] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[NetwatchOutageLog] CHECK CONSTRAINT [FK_NetwatchOutageLog_NetwatchConfigs]
GO
ALTER TABLE [dbo].[NotificationHistoryLogs]  WITH CHECK ADD  CONSTRAINT [FK_NotificationHistoryLogs_NotificationRules] FOREIGN KEY([RuleId])
REFERENCES [dbo].[NotificationRules] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[NotificationHistoryLogs] CHECK CONSTRAINT [FK_NotificationHistoryLogs_NotificationRules]
GO
ALTER TABLE [dbo].[RolePermissions]  WITH CHECK ADD  CONSTRAINT [FK_RolePermissions_UserRoles] FOREIGN KEY([RoleId])
REFERENCES [dbo].[UserRoles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RolePermissions] CHECK CONSTRAINT [FK_RolePermissions_UserRoles]
GO
ALTER TABLE [dbo].[UserAccounts]  WITH CHECK ADD  CONSTRAINT [FK_UserAccounts_UserRoles] FOREIGN KEY([RoleId])
REFERENCES [dbo].[UserRoles] ([Id])
GO
ALTER TABLE [dbo].[UserAccounts] CHECK CONSTRAINT [FK_UserAccounts_UserRoles]
GO
USE [master]
GO
ALTER DATABASE [CustomerAndServerMaintenanceTracking] SET  READ_WRITE 
GO
