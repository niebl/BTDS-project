const mongoose = require('mongoose');

const AoiSchema = new mongoose.Schema({
    sensorID: {
        type: String,
        index: true,
        required: true,
        unique: true
    },
    description:{
        type: String
    },
    inRoom:{
        type: mongoose.Schema.Types.ObjectId, 
        ref: 'Room',
        default: null
    },

    //the observation of the Aoi, as it is updated in regular intervals
    observation:{
        type: Object
    }

})

module.exports = mongoose.model('Aoi', AoiSchema)