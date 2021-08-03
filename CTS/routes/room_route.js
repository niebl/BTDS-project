const express = require('express');
const router = express.Router();

const Room = require('./../models/room');
const Passage = require('./../models/passage');
const Aoi = require('./../models/aoi');

//search for rooms by roomName
//defaults to seach for all
router.get('/', async (req,res) => {

    let roomNr = req.query.roomnr || null;
    //let _id = req.query.id || null

    let query = {
    }

    if(roomNr != null){
        query.roomNumber = roomNr
    }

    const rooms = await Room.find(query);

    res.send(rooms);
})

module.exports = router;