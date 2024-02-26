namespace Refresh.GameServer.Authentication;

public static class TokenGameUtility
{
    // Most values taken from SerialStation.
    // https://serialstation.com
    
    private static readonly string[] LittleBigPlanet1Titles =
    {
        "NPUA80472", // US Digital
        "BCUS98148", // US Disc
        "BCUS98199", // US Disc? Comes with 02.00 apparently. Maybe never sold?
        "BCUS98208", // US "Game of the Year" Disc
        
        "BCES00141", // EU Disc
        "NPEA00241", // EU Digital
        "BCES00611", // EU "Game of the Year" Disc
        
        "BCAS20058", // Asia Disc
        "BCAS20078", // Asia "Game of the Year" Disc
        
        "BCKS10059", // Korea Disc
        "NPHA80092", // Korea Digital
        
        "NPJA00052", // JP Digital
        "BCJS30018", // JP Disc
        
        // Betas/Debug/Prerelease
        "BCET70002", // EU "Online Trial" Beta Test
        "BCET70011", // EU Water Beta Test
        "NPUA70045", // US Demo
    };

    private static readonly string[] LittleBigPlanet2Titles =
    {
        "NPUA80662", // US Digital
        "BCUS98245", // US Disc
        "BCUS98249", // US Collector's Edition Disc (hey, that's me!)
        "BCUS90260", // US Special Edition Disc
        "BCUS98372", // US Special Edition Disc
        
        "NPEA00324", // EU Digital
        "BCES00850", // EU Disc
        "BCES01345", // EU Special Edition Disc
        "BCES01694", // EU Extras Edition Disc
        
        "BCES01086", // UK Disc
        "BCES01346", // UK Special Edition Disc
        
        // missing asia disc?
        // missing asia digital?
        "BCAS20201", // Asia Special Edition Disc
        
        "BCJS30058", // JP Disc
        // missing japan digital?
        
        // Betas/Debug/Prerelease
        "NPUA70117", // US Private Beta,
        
        "BCET70055", // Hub,
        "NPEA00449",
    };

    // PS4 title ids are not here on purpose.
    private static readonly string[] LittleBigPlanet3Titles =
    {
        "BCUS98362", // US Disc
        "BCUS81138", // US Disc (another? v01.01)
        
        "BCES01663", // EU Disc
        "NPEA00515", // EU Digital
        
        "BCES02068", // UK Disc
        
        "BCAS20322", // Asia Disc
        
        "BCJS30095", // JP Disc
        "NPJA00123", // JP Digital
    };

    private static readonly string[] LittleBigPlanetPSPTitles =
    {
        "UCUS98744", // US UMD
        "UCES01264", // EU UMD
        "UCJS10107", // JP UMD
        "UCAS40262", // Asia UMD
        
        "NPJG00073", // JP Digital
        "NPHG00033", // Asia Digital
        
        // Betas/Debug/Prerelease
        "NPUG70064", // US Demo
        "NPEG90019", // EU Demo
        "NPHG00035", // Asia Demo
    };

    private static readonly string[] LittleBigPlanetVitaTitles =
    {
        "PCSA22018", // US Cartridge
        "PCSA00017", // US Digital
        
        "PCSF00021", // EU Cartridge
        "PCSA22106", // Canada Cartridge
        
        "PCSD00006", // Asia Cartridge
        "VCAS32010", // Asia Cartridge
        
        "PCSC00013", // JP Digital
        "VCJS10006", // JP Cartridge
        
        // Betas/Debug/Prerelease
        "PCSA00061", // US Beta
        "PCSF00152", // EU Beta
    };
    
    public static TokenGame? FromTitleId(string titleId)
    {
        if (LittleBigPlanet1Titles.Contains(titleId)) return TokenGame.LittleBigPlanet1;
        if (LittleBigPlanet2Titles.Contains(titleId)) return TokenGame.LittleBigPlanet2;
        if (LittleBigPlanet3Titles.Contains(titleId)) return TokenGame.LittleBigPlanet3;
        if (LittleBigPlanetPSPTitles.Contains(titleId)) return TokenGame.LittleBigPlanetPSP;
        if (LittleBigPlanetVitaTitles.Contains(titleId)) return TokenGame.LittleBigPlanetVita;
        
        return null;
    }
}