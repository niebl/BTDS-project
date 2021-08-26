const mongoose = require('mongoose');

const PassageSchema = new mongoose.Schema({
    sensorID: {
        type: String,
        index: true,
        required: true,
        unique: true
    },
    toRoom:{
        type: mongoose.Schema.Types.ObjectId, 
        ref: 'Room',
        default: null
    },
    fromRoom:{
        type: mongoose.Schema.Types.ObjectId, 
        ref: 'Room',
        default: null
    }
});

module.exports = mongoose.model('Passage', PassageSchema)