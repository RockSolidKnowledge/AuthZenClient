const path = require('path');

module.exports = {
    entry: "./src/index.ts",
    output: {
        path: path.join(__dirname, "dist"),
        filename: "index.js",
        library: 'AuthZenClient',
        libraryTarget: 'window',
    },
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: 'ts-loader',
                exclude: /node_modules/,
            },
        ],
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
    },
    externals: {
        fetch: 'fetch'
    },
};