const express = require('express');
var app = express();
const mongoose = require('mongoose');
var expressWs = require('express-ws')(app);

app.use(express.urlencoded({ extended: false }));
require('dotenv').config()

mongoose.connect(process.env.MONGO_CONNECTION,{
    useNewUrlParser: true,
    useUnifiedTopology : true,
    useCreateIndex: true
})

const guiRouter = require('./routes/gui_route')
const configRouter = require('./routes/config_route')
const roomRouter = require('./routes/room_route')
const sensorRouter = require('./routes/sensor_route')


app.set('view engine', 'ejs');
app.use("/", guiRouter);
app.use("/config", configRouter);
app.use("/room", roomRouter);
app.use("/sensor", sensorRouter);

app.use(express.static('public'))


app.ws('/', function(ws, req) {
    ws.on('message', function(msg) {
      console.log(msg);
    });
    console.log('socket', req.testing);
  }); 

app.listen(process.env.SERVER_PORT);