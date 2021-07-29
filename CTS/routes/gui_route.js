const express = require('express');
const router = express.Router();

const Room = require('./../models/room');
const Passage = require('./../models/passage');
const Aoi = require('./../models/aoi');


/**
 * this get-endpoint supplies the webpage overview of the CTS
 */
router.get('/', async (req,res) => {
    //TODO: this will be an Object that contains a representation of all the rooms we can view
    const building = await Room.find();

    for(room of building){
        await populatePss(room);
        await populateRSS(room);
    }

    res.render('index', {rooms: building});
})

async function populatePss(room){
    for(pss in room.passages){
        room.passages[pss] = await Passage.findById(room.passages[pss]._id);
    }
};

async function populateRSS(room){
    room.aoi = await Aoi.findById(room.aoi);
};

    
module.exports = router;