﻿open System
open System.IO
open FSharp.Configuration
open FSharp.Data

let debugDir = Environment.CurrentDirectory
let binDir = Directory.GetParent(debugDir)
let simpsonsEpisodesCodeDir = Directory.GetParent(binDir.FullName)
let simpsonsEpisodesRepoDir = Directory.GetParent(simpsonsEpisodesCodeDir.FullName)
let dataDirectory = Path.Combine(simpsonsEpisodesRepoDir.FullName, "Data")
let seasonsDataDirectory = Path.Combine(dataDirectory, "Seasons")

let episodeUrlStart = "http://en.wikipedia.org/wiki/The_Simpsons_(season_"
let currentNumberOfSeries = 26

let getSeasonFileName = fun (seasonNumber: int) -> 
    Path.Combine( seasonsDataDirectory, "Season_" + seasonNumber.ToString() + ".html")

let downloadSeasonFilesToDisk =
    if Directory.Exists(dataDirectory) = false then
        Directory.CreateDirectory(dataDirectory) |> ignore

    if Directory.Exists(seasonsDataDirectory) = false then
        Directory.CreateDirectory(seasonsDataDirectory) |> ignore

    Console.WriteLine("Downloading Files if need be")
    for seriesNumber in 1 .. currentNumberOfSeries do
        let seasonFileName = getSeasonFileName(seriesNumber)
        if File.Exists(getSeasonFileName(seriesNumber)) then
            Console.WriteLine("File exists at:" + seasonFileName)
        else
            let seasonUrl = episodeUrlStart + seriesNumber.ToString() + ")"
            Console.WriteLine("Url for season is: " + seasonUrl)
            let seasonHtml = HtmlDocument.Load(seasonUrl)
            File.WriteAllLines(seasonFileName, [seasonHtml.ToString()])
            Console.WriteLine("Downloaded to file: " + seasonFileName)

let getEpisodeTableHtmlForSeason seasonNumber: HtmlNode =
    let seasonHtml = HtmlDocument.Load(getSeasonFileName(seasonNumber))
    let tables = seasonHtml.Descendants["table"]
    // Episodes are shown in a table with the classes wikitable plainrowheaders for some reason
    // Season 27 has two tables like this, but it isn't out yet so not going to worry about it 
    let episodeTableClass = "wikitable plainrowheaders"
    let episodeTables = 
        tables
        |> Seq.filter (fun (t: HtmlNode) -> 
            t.HasClass(episodeTableClass)        
        )
    if Seq.length episodeTables <> 1 then
        raise (new Exception("Can't find a episodes table in the season"))
    Seq.head episodeTables

type EpisodeInfo(seasonNumber: int, episodeNumber:int, wikiUrlSuffix:string, description: HtmlNode) =
    member this.seasonNumber = seasonNumber
    member this.episodeNumber = episodeNumber
    member this.wikiUrlSuffix = wikiUrlSuffix
    member this.description = description

[<EntryPoint>]
let main argv = 
    
    downloadSeasonFilesToDisk
    
    for seriesNumber in 1 .. currentNumberOfSeries do
        let episodeTableHtml = getEpisodeTableHtmlForSeason seriesNumber
        // Episode tables have a header then pairs of informations and descriptions.
        let trElements = episodeTableHtml.Descendants["tr"]
        
        let header = trElements |> Seq.head
        let infoAndDescriptions =  trElements |> Seq.filter (fun (a) -> a <> header )
        let infosAndDescriptionsList = infoAndDescriptions |> Seq.toList
        let numberOfInfosAndDecriptions = (List.length infosAndDescriptionsList)
        if numberOfInfosAndDecriptions % 2 <> 0 then
            raise (Exception("This should be an even number!"))
        
        let numberOfEpisodes = numberOfInfosAndDecriptions / 2

        for episodeNumber in 0 .. (numberOfEpisodes - 1) do
            let infoElement = infosAndDescriptionsList.[episodeNumber * 2]
            if infoElement.HasAttribute("class", "vevent") = false then
                raise(Exception("This is not an info row when it should be!"))

            let episodeWikiUrlAnchor = 
                infoElement.Elements().[2].Descendants["a"]
                |> Seq.head
            let episodeWikiHref = episodeWikiUrlAnchor.AttributeValue("href")
            
            let descriptionRowElement = infosAndDescriptionsList.[episodeNumber * 2 + 1]
            let descriptionElement = descriptionRowElement.Descendants("td") |> Seq.head
            if descriptionElement.HasAttribute("class", "description") = false then
                raise(Exception("This is not a description row when it should be!"))
            let descText = descriptionElement.InnerText
            let descString = descriptionElement.ToString()
            let episodeInfo = EpisodeInfo(seriesNumber, episodeNumber + 1, episodeWikiHref, descriptionElement)

            0 |> ignore

        let thing = 0

        
        Console.WriteLine("Loaded File")
        
    Console.ReadKey() |> ignore
    0 // return an integer exit code
