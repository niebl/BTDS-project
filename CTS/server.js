require('dotenv').config()

const express = require('express');
const app = express();

const guiRouter = require('./routes/gui_route')

app.set('view engine', 'ejs');
app.use("/",guiRouter);

app.listen(process.env.SERVER_PORT);