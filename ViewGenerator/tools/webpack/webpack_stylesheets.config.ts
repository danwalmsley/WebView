﻿import MiniCssExtractPlugin from "mini-css-extract-plugin";
import { join, resolve } from "path";
import { Configuration } from "webpack";

import MiniCssExtractPluginCleanup from "./Plugins/MiniCssExtractPluginCleanup";
import { CssPlaceholder, JsMapPlaceholder, OutputDirectoryDefault } from "./Plugins/Resources";
import { Dictionary, getCurrentDirectory } from "./Plugins/Utils"

import getResourcesRuleSet from "./Rules/Files";
import SassRuleSet from "./Rules/Sass";

const config = (_, argv) => {

    let projectDir = argv.projectDir ? resolve(argv.projectDir) : "";

    const getEntryName = (entryPath: string): string => {
        let fileExtensionLen: number = entryPath.length - entryPath.lastIndexOf(".");
        return entryPath.slice(entryPath.lastIndexOf("\\") + 1, -fileExtensionLen);
    };

    let entries: string = argv.entryPath;
    let entryMap: Dictionary<string> = {};
    entries.split(";").map(entryPath => entryMap[getEntryName(entryPath)] = './' + entryPath)
    
    let stylesheetsConfig: Configuration = {
        entry: entryMap,

        output: {
            path: getCurrentDirectory(),
            filename: JsMapPlaceholder
        },

        resolveLoader: {
            modules: [ join(__dirname, "/node_modules") ],
        },

        module: {
            rules: [
                SassRuleSet,
                getResourcesRuleSet(projectDir)
            ]
        },

        plugins: [
            new MiniCssExtractPlugin({ filename: OutputDirectoryDefault + CssPlaceholder }),
            new MiniCssExtractPluginCleanup([/\.js.map$/])
        ]
    }

    return stylesheetsConfig;
};

export default config;
