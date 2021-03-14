const dotenv = require("dotenv");
const webSocketServer = require("websocket").server;
const http = require("http");



dotenv.config();

const server = http.createServer();
server.listen(process.env.PORT);

const DiscordHelperServer = class DiscordHelperserver extends webSocketServer {
    constructor(httpS, autoAcceptConnections = false) {
        super({httpServer: httpS, autoAcceptConnections: false});

        this._clients = {};
        this.util = require("util");

        this._setupServer();
        
    }

    getUniqueID() {
      //todo run check on if uniqueID already assigned
      const s4 = () => Math.floor((1 + Math.random()) * 0x10000).toString(16).substring(1);
      return s4() + s4() + '-' + s4();
    }

    _setupServer() {
        this.on("request", function(request) {
            debugger;
            var uniqueID;
            if (uniqueID = request.resourceURL.query.uniqueID == null) {
                //Assign uniqueID if server does not provide;
                uniqueID = this.getUniqueID();
            } else {
            }
            const connection = request.accept(null, request.origin);
            this._clients[uniqueID] = connection;
        })
    }
}

const wsServer = new DiscordHelperServer(server);